using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoiceInfo.DTOs
{
    public class CommentCreateDto
    {
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        [Required]
        public int PostId { get; set; }

        public int? ParentCommentId { get; set; }
    }

    public class CommentUpdateDto
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        [Required]
        public string UserId { get; set; }
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int? ParentCommentId { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new List<CommentResponseDto>();
    }
}