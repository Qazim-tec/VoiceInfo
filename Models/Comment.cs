using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceInfo.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)] // Added maximum length constraint
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false; // Soft delete support

        // Relationships
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User Commenter { get; set; }

        [Required]
        public int PostId { get; set; }

        [ForeignKey("PostId")]
        public Post Post { get; set; }

        // Parent-child comments for nested replies
        public int? ParentCommentId { get; set; }
        public Comment ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public int CommentsCount { get; set; }
    }
}