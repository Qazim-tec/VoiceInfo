using VoiceInfo.DTOs;

namespace VoiceInfo.IService
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterAsync(UserRegisterDto userRegisterDto);
        Task<UserResponseDto> UpdateUserAsync(string userId, UserUpdateDto userUpdateDto);
        Task<UserResponseDto> GetUserByIdAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> AssignRoleAsync(string userId, string role);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto); // Add this method
        Task<UserProfileStatsDto> GetUserProfileStatsAsync(string userId);

    }

}
