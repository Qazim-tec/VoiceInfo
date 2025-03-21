using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Services;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateComment([FromBody] CommentCreateDto commentCreateDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var comment = await _commentService.CreateCommentAsync(commentCreateDto, userId);
                return Ok(new { data = comment, message = "Comment created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create comment", details = ex.Message });
            }
        }

        [HttpPut("update/{commentId}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] CommentUpdateDto commentUpdateDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var comment = await _commentService.UpdateCommentAsync(commentId, commentUpdateDto, userId);
                return Ok(new { data = comment, message = "Comment updated successfully" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found") || ex.Message.Contains("unauthorized"))
                {
                    return StatusCode(403, new { error = ex.Message });
                }
                return StatusCode(500, new { error = "Failed to update comment", details = ex.Message });
            }
        }

        [HttpGet("{commentId}")]
        public async Task<IActionResult> GetComment(int commentId)
        {
            try
            {
                var comment = await _commentService.GetCommentByIdAsync(commentId);
                return Ok(new { data = comment });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                return StatusCode(500, new { error = "Failed to retrieve comment", details = ex.Message });
            }
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsByPostId(int postId)
        {
            try
            {
                var comments = await _commentService.GetCommentsByPostIdAsync(postId);
                return Ok(new { data = comments });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve comments", details = ex.Message });
            }
        }

        [HttpDelete("delete/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var result = await _commentService.DeleteCommentAsync(commentId, userId);
                return result
                    ? Ok(new { message = "Comment deleted successfully" })
                    : NotFound(new { error = "Comment not found" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete comment", details = ex.Message });
            }
        }
    }
}