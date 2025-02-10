using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoiceInfo.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)] // Added maximum length constraint
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false; // Soft delete support

        // Navigation property for posts with this tag
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}