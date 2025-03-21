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
        private readonly ApplicationDbContext _context; // Added for Posts and Comments
        private readonly IMemoryCache _cache; // Added for caching
        private const string ProfileStatsCacheKeyPrefix = "profile_stats_";
        private readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public UserService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context, // Injected
            IMemoryCache cache) // Injected
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<UserResponseDto> RegisterAsync(UserRegisterDto userRegisterDto)
        {
            var user = new User
            {
                FirstName = userRegisterDto.FirstName,
                LastName = userRegisterDto.LastName,
                Email = userRegisterDto.Email,
                UserName = userRegisterDto.Email
            };

            var result = await _userManager.CreateAsync(user, userRegisterDto.Password);
            if (!result.Succeeded)
                throw new Exception("User registration failed.");

            await _userManager.AddToRoleAsync(user, "User");

            return new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
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
            _cache.Remove($"{ProfileStatsCacheKeyPrefix}{userId}"); // Invalidate cache on update

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
            _cache.Remove($"{ProfileStatsCacheKeyPrefix}{userId}"); // Invalidate cache on delete
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

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
                throw new Exception("Invalid email or password.");

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

        public async Task<UserProfileStatsDto> GetUserProfileStatsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            string cacheKey = $"{ProfileStatsCacheKeyPrefix}{userId}";
            if (_cache.TryGetValue(cacheKey, out UserProfileStatsDto cachedStats))
            {
                return cachedStats; 
            }

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

            var stats = new UserProfileStatsDto
            {
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email,
                PostsCount = postsCount,
                CommentsCount = commentsCount
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration); // 10-minute cache
            _cache.Set(cacheKey, stats, cacheOptions);

            return stats;
        }
    }
}