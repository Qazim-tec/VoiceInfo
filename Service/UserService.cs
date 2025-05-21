using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Models;

namespace VoiceInfo.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public UserService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task RegisterAsync(UserRegisterDto userRegisterDto)
        {
            // Check if email already exists in actual user table
            var existingUser = await _userManager.FindByEmailAsync(userRegisterDto.Email);
            if (existingUser != null)
                throw new Exception("Email already registered.");

            // Check if email is in cache
            if (_cache.TryGetValue($"TempUser_{userRegisterDto.Email}", out _))
                throw new Exception("Registration already in progress. Please check your email for the OTP.");

            // Generate OTP
            var otp = GenerateOtp();

            // Hash the password
            var passwordHasher = new PasswordHasher<User>();
            var tempData = new TempRegistrationData
            {
                FirstName = userRegisterDto.FirstName,
                LastName = userRegisterDto.LastName,
                Email = userRegisterDto.Email,
                PasswordHash = passwordHasher.HashPassword(null, userRegisterDto.Password),
                OtpCode = otp,
                OtpExpiration = DateTime.UtcNow.AddMinutes(10)
            };

            // Store in cache with 10-minute expiration
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = tempData.OtpExpiration
            };
            _cache.Set($"TempUser_{userRegisterDto.Email}", tempData, cacheEntryOptions);

            // Send OTP email
            await _emailService.SendVerificationOtpAsync(tempData.Email, tempData.OtpCode);
        }

        public async Task<AuthResponseDto> ConfirmOtpAsync(ConfirmOtpDto confirmOtpDto)
        {
            // Retrieve temporary user data from cache
            if (!_cache.TryGetValue($"TempUser_{confirmOtpDto.Email}", out TempRegistrationData tempData))
                throw new Exception("User not found or registration expired.");

            if (tempData.OtpCode != confirmOtpDto.OtpCode || tempData.OtpExpiration < DateTime.UtcNow)
                throw new Exception("Invalid or expired OTP.");

            // Create actual user
            var user = new User
            {
                FirstName = tempData.FirstName,
                LastName = tempData.LastName,
                Email = tempData.Email,
                UserName = tempData.Email,
                IsEmailVerified = true
            };

            // Create user without password initially
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new Exception("User creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

            // Set the password hash directly
            user.PasswordHash = tempData.PasswordHash;
            await _userManager.UpdateAsync(user);

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            // Remove from cache immediately after confirmation
            _cache.Remove($"TempUser_{confirmOtpDto.Email}");

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? "User")
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new AuthResponseDto
            {
                Token = tokenString,
                UserId = user.Id,
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? "User",
                FirstName = user.FirstName
            };
        }

        public async Task ResendOtpAsync(string email)
        {
            // Check if email is in cache
            if (!_cache.TryGetValue($"TempUser_{email}", out TempRegistrationData tempData))
                return; // Silently fail to prevent email enumeration

            // Generate new OTP
            var newOtp = GenerateOtp();
            tempData.OtpCode = newOtp;
            tempData.OtpExpiration = DateTime.UtcNow.AddMinutes(10);

            // Update cache
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = tempData.OtpExpiration
            };
            _cache.Set($"TempUser_{email}", tempData, cacheEntryOptions);

            // Send new OTP email
            await _emailService.SendVerificationOtpAsync(tempData.Email, tempData.OtpCode);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
                throw new Exception("Invalid email or password.");

            if (!user.IsEmailVerified)
                throw new Exception("Email not verified. Please confirm your OTP.");

            var roles = await _userManager.GetRolesAsync(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? "User")
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new AuthResponseDto
            {
                Token = tokenString,
                UserId = user.Id,
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? "User",
                FirstName = user.FirstName
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null)
                return; // Silently fail to prevent email enumeration

            // Generate and store OTP
            user.OtpCode = GenerateOtp();
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            // Send OTP email
            await _emailService.SendResetPasswordOtpAsync(user.Email, user.OtpCode);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                throw new Exception("User not found.");

            if (user.OtpCode != resetPasswordDto.OtpCode || user.OtpExpiration < DateTime.UtcNow)
                throw new Exception("Invalid or expired OTP.");

            // Reset password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, resetPasswordDto.NewPassword);
            if (!result.Succeeded)
                throw new Exception("Password reset failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

            // Clear OTP
            user.OtpCode = null;
            user.OtpExpiration = null;
            await _userManager.UpdateAsync(user);
        }

        public async Task<UserResponseDto> UpdateUserAsync(string userId, UserUpdateDto userUpdateDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            user.FirstName = userUpdateDto.FirstName;
            user.LastName = userUpdateDto.LastName;
            user.Email = userUpdateDto.Email;
            user.ProfilePicture = userUpdateDto.ProfilePicture;

            await _userManager.UpdateAsync(user);

            return new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserResponseDto> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            return new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            user.IsDeleted = true;
            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<bool> AssignRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists)
                throw new Exception("Role does not exist.");

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw new Exception("Failed to assign role.");

            return true;
        }

        public async Task<UserProfileStatsDto> GetUserProfileStatsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var postsCount = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .CountAsync();

            var commentsCount = await _context.Comments
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .CountAsync();

            return new UserProfileStatsDto
            {
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email,
                PostsCount = postsCount,
                CommentsCount = commentsCount
            };
        }
    }
}