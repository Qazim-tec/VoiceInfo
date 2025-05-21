using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VoiceInfo.DTOs
{
    public class PostCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; }

        [MaxLength(500)]
        public string? Excerpt { get; set; } // Already optional

        public IFormFile? FeaturedImage { get; set; } // Already optional

        // Add up to 3 additional images
        [MaxLength(3, ErrorMessage = "You can upload up to 3 additional images.")]
        public List<IFormFile> AdditionalImages { get; set; } = new List<IFormFile>();

        public int? CategoryId { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class PostUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; }

        [MaxLength(500)]
        public string? Excerpt { get; set; } // Already optional

        public IFormFile? FeaturedImage { get; set; } // Already optional

        // Add up to 3 additional images
        [MaxLength(3, ErrorMessage = "You can upload up to 3 additional images.")]
        public List<IFormFile> AdditionalImages { get; set; } = new List<IFormFile>();

        public List<string> AdditionalImagesToDelete { get; set; } = new List<string>();

        public int? CategoryId { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; } // Already optional
        public List<string> AdditionalImageUrls { get; set; } = new List<string>(); // Add this
        public int Views { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsLatestNews { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Slug { get; set; }

        public string AuthorId { get; set; }
        public string AuthorName { get; set; }

        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
        public int CommentsCount { get; set; }
        public List<CommentResponseDto> Comments { get; set; } = new List<CommentResponseDto>();
        public int LikesCount { get; set; }
        public bool IsLikedByUser { get; set; }
    }

    public class PostLightDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsLatestNews { get; set; }
        public int LikesCount { get; set; }
    }

    public class FeaturePostDto
    {
        [Required]
        public int PostId { get; set; }

        [Required]
        public bool IsFeatured { get; set; }
    }

    public class LatestNewsPostDto
    {
        [Required]
        public int PostId { get; set; }

        [Required]
        public bool IsLatestNews { get; set; }
    }
}