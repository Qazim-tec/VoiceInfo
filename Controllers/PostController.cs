using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Added for DbContext
using System.Security.Claims;
using System.Threading.Tasks;
using VoiceInfo.Data; // Added for ApplicationDbContext
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
        private readonly ApplicationDbContext _context; // Added for PostLikes queries

        public PostController(IPostService postService, ApplicationDbContext context)
        {
            _postService = postService;
            _context = context;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] PostCreateDto postCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid post data", details = ModelState });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var post = await _postService.CreatePostAsync(postCreateDto, userId);
                return Ok(new { data = post, message = "Post created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create post", details = ex.Message });
            }
        }

        [HttpPut("update/{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int postId, [FromForm] PostUpdateDto postUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid post data", details = ModelState });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var post = await _postService.UpdatePostAsync(postId, postUpdateDto, userId);
                return Ok(new { data = post, message = "Post updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update post", details = ex.Message });
            }
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPost(int postId)
        {
            try
            {
                var post = await _postService.GetPostByIdAsync(postId);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    post.IsLikedByUser = await _context.PostLikes.AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);
                }
                return Ok(new { data = post });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve post", details = ex.Message });
            }
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetPostBySlug(string slug)
        {
            try
            {
                var post = await _postService.GetPostBySlugAsync(slug);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    post.IsLikedByUser = await _context.PostLikes.AnyAsync(pl => pl.PostId == post.Id && pl.UserId == userId);
                }
                return Ok(new { data = post });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve post", details = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var posts = await _postService.GetAllPostsAsync();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    foreach (var post in posts)
                    {
                        post.IsLikedByUser = await _context.PostLikes.AnyAsync(pl => pl.PostId == post.Id && pl.UserId == userId);
                    }
                }
                return Ok(new { data = posts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve posts", details = ex.Message });
            }
        }

        [HttpDelete("delete/{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var result = await _postService.DeletePostAsync(postId, userId);
                return result
                    ? Ok(new { message = "Post deleted successfully" })
                    : NotFound(new { error = "Post not found or unauthorized" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete post", details = ex.Message });
            }
        }

        [HttpPut("feature/{postId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FeaturePost(int postId, [FromQuery] bool isFeatured)
        {
            try
            {
                var result = await _postService.FeaturePostAsync(postId, isFeatured);
                return result
                    ? Ok(new { message = "Post feature status updated" })
                    : NotFound(new { error = "Post not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update feature status", details = ex.Message });
            }
        }

        [HttpPut("latest-news/{postId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetLatestNews(int postId, [FromQuery] bool isLatestNews)
        {
            try
            {
                var result = await _postService.SetLatestNewsAsync(postId, isLatestNews);
                return result
                    ? Ok(new { message = "Post latest news status updated" })
                    : NotFound(new { error = "Post not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update latest news status", details = ex.Message });
            }
        }

        [HttpGet("all-posts-light")]
        public async Task<IActionResult> GetAllPostsLight([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
        {
            try
            {
                var result = await _postService.GetAllPostsLightAsync(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost("like/{postId}")]
        [Authorize]
        public async Task<IActionResult> LikePost(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var result = await _postService.LikePostAsync(postId, userId);
                return result
                    ? Ok(new { message = "Post liked successfully" })
                    : BadRequest(new { error = "Post already liked" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to like post", details = ex.Message });
            }
        }

        [HttpPost("unlike/{postId}")]
        [Authorize]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            try
            {
                var result = await _postService.UnlikePostAsync(postId, userId);
                return result
                    ? Ok(new { message = "Post unliked successfully" })
                    : BadRequest(new { error = "Post not liked by user" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to unlike post", details = ex.Message });
            }
        }
    }
}