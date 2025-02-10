using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceInfo.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)] // Added maximum length constraint

        
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false; // Soft delete support

        // Navigation property for posts in this category
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}