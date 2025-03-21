using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;

        public PostController(IPostService postService, IMemoryCache cache)
        {
            _postService = postService;
            _cache = cache;
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
            string cacheKey = $"post_{postId}";
            if (_cache.TryGetValue(cacheKey, out PostResponseDto cachedPost))
            {
                return Ok(new { data = cachedPost });
            }

            try
            {
                var post = await _postService.GetPostByIdAsync(postId);
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
            string cacheKey = $"post_slug_{slug}";
            if (_cache.TryGetValue(cacheKey, out PostResponseDto cachedPost))
            {
                return Ok(new { data = cachedPost });
            }

            try
            {
                var post = await _postService.GetPostBySlugAsync(slug);
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
            string cacheKey = "all_posts";
            if (_cache.TryGetValue(cacheKey, out var cachedPosts))
            {
                return Ok(new { data = cachedPosts });
            }

            try
            {
                var posts = await _postService.GetAllPostsAsync();
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
    }
}