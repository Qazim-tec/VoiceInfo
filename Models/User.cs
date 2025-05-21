using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoiceInfo.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)] // Added maximum length constraint
        public string FirstName { get; set; }

        [MaxLength(50)] // Added maximum length constraint
        public string LastName { get; set; } // Added last name support

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)] // Added maximum length constraint
        public string? ProfilePicture { get; set; } // Profile picture URL

        public bool IsDeleted { get; set; } = false; // Soft delete support

        // OTP-related fields
        public string? OtpCode { get; set; } // Store the OTP
        public DateTime? OtpExpiration { get; set; } // OTP expiration time
        public bool IsEmailVerified { get; set; } = false; // Track email verification

        // Navigation properties
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

    }
}