using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Services;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] PostCreateDto postCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var post = await _postService.CreatePostAsync(postCreateDto, userId);
            return Ok(post);
        }

        [HttpPut("update/{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int postId, [FromForm] PostUpdateDto postUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var post = await _postService.UpdatePostAsync(postId, postUpdateDto, userId);
            return Ok(post);
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPost(int postId)
        {
            var post = await _postService.GetPostByIdAsync(postId);
            return Ok(post);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPosts()
        {
            var posts = await _postService.GetAllPostsAsync();
            return Ok(posts);
        }

        [HttpDelete("delete/{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var result = await _postService.DeletePostAsync(postId, userId);
            return Ok(result);
        }

        [HttpPut("feature/{postId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FeaturePost(int postId, [FromQuery] bool isFeatured)
        {
            var result = await _postService.FeaturePostAsync(postId, isFeatured);
            return Ok(result);
        }

        [HttpPut("latest-news/{postId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetLatestNews(int postId, [FromQuery] bool isLatestNews)
        {
            var result = await _postService.SetLatestNewsAsync(postId, isLatestNews);
            return result ? Ok(new { message = "Post updated successfully" }) : NotFound("Post not found.");
        }



    }
}