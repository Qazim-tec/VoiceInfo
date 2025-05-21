using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace VoiceInfo.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; }

        [MaxLength(500)]
        public string? Excerpt { get; set; } 

        public string? FeaturedImageUrl { get; set; } 

        // Add this to store up to 3 additional image URLs
        public List<string> AdditionalImageUrls { get; set; } = new List<string>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Views { get; set; } = 0;
        public bool IsFeatured { get; set; } = false;
        public bool IsLatestNews { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; }

        public string UserId { get; set; }
        public User Author { get; set; }

        public int? CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        public int LikesCount { get; set; } = 0;
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

        public void GenerateSlug(int postId = 0)
        {
            Slug = SlugGenerator.GenerateSlug(Title, postId);
        }
    }


    public static class SlugGenerator
    {
        public static string GenerateSlug(string title, int postId = 0)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            string slug = title.ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');

            // If there's a postId, append it to make the slug unique
            if (postId > 0)
            {
                slug = $"{slug}-{postId}";
            }

            return slug;
        }
    }
}
