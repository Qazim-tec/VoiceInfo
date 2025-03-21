using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
using VoiceInfo.IService;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto userRegisterDto)
        {
            var user = await _userService.RegisterAsync(userRegisterDto);
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var authResponse = await _userService.LoginAsync(loginDto);
            return Ok(authResponse);
        }

        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, UserUpdateDto userUpdateDto)
        {
            var user = await _userService.UpdateUserAsync(userId, userUpdateDto);
            return Ok(user);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return Ok(user);
        }

        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            return Ok(result);
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentDto roleAssignmentDto)
        {
            var result = await _userService.AssignRoleAsync(roleAssignmentDto.UserId, roleAssignmentDto.Role);
            return Ok(result);
        }

        [HttpGet("profile-stats")]
        [Authorize]
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)] // 10-minute HTTP cache
        public async Task<IActionResult> GetProfileStats()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token.");

                var stats = await _userService.GetUserProfileStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}