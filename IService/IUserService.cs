using VoiceInfo.DTOs;

namespace VoiceInfo.IService
{
    public interface IUserService
    {
        Task RegisterAsync(UserRegisterDto userRegisterDto);
        Task<AuthResponseDto> ConfirmOtpAsync(ConfirmOtpDto confirmOtpDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserResponseDto> UpdateUserAsync(string userId, UserUpdateDto userUpdateDto);
        Task<UserResponseDto> GetUserByIdAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> AssignRoleAsync(string userId, string role);
        Task<UserProfileStatsDto> GetUserProfileStatsAsync(string userId);
        Task ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task ResendOtpAsync(string email);

    }

}
