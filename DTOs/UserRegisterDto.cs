using System.ComponentModel.DataAnnotations;

namespace VoiceInfo.DTOs
{
    public class UserRegisterDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class UserUpdateDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [MaxLength(50)]
        public string LastName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string ProfilePicture { get; set; }
    }

    public class UserResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RoleDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be either 'Admin' or 'User'.")]
        public string Role { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }
        public string FirstName { get; set; }
        


    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; }
    }

    public class RoleAssignmentDto
    {
        [Required]
        public string UserId { get; set; } // The ID of the user to assign the role to

        [Required]
        public string Role { get; set; } // The role to assign (e.g., "Admin" or "User")
    }

    public class UserProfileStatsDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public int PostsCount { get; set; }
        public int CommentsCount { get; set; }
    }
}