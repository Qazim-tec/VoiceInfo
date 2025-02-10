using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
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
        public async Task<IActionResult> CreateComment(CommentCreateDto commentCreateDto, [FromHeader] string userId)
        {
            var comment = await _commentService.CreateCommentAsync(commentCreateDto, userId);
            return Ok(comment);
        }

        [HttpPut("update/{commentId}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(int commentId, CommentUpdateDto commentUpdateDto, [FromHeader] string userId)
        {
            var comment = await _commentService.UpdateCommentAsync(commentId, commentUpdateDto, userId);
            return Ok(comment);
        }

        [HttpGet("{commentId}")]
        public async Task<IActionResult> GetComment(int commentId)
        {
            var comment = await _commentService.GetCommentByIdAsync(commentId);
            return Ok(comment);
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsByPostId(int postId)
        {
            var comments = await _commentService.GetCommentsByPostIdAsync(postId);
            return Ok(comments);
        }

        [HttpDelete("delete/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId, [FromHeader] string userId)
        {
            var result = await _commentService.DeleteCommentAsync(commentId, userId);
            return Ok(result);
        }
    }
}