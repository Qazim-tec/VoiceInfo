using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Services;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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
        [Authorize(Roles = "Admin")] // Only admins can assign roles
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentDto roleAssignmentDto)
        {
            var result = await _userService.AssignRoleAsync(roleAssignmentDto.UserId, roleAssignmentDto.Role);
            return Ok(result);
        }
    }
}