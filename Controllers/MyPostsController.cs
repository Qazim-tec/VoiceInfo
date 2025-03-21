using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceInfo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MyPostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const int PostsPerPage = 15;

        public MyPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/MyPosts?page=1
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<PostResponseDto>>> GetMyPosts([FromQuery] int page = 1)
        {
            if (page < 1) page = 1;

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var totalPosts = await _context.Posts
                .CountAsync(p => p.UserId == userId && !p.IsDeleted);

            var totalPages = (int)Math.Ceiling(totalPosts / (double)PostsPerPage);

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * PostsPerPage)
                .Take(PostsPerPage)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Excerpt = p.Excerpt,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    Views = p.Views,
                    IsLatestNews = p.IsLatestNews,
                    IsFeatured = p.IsFeatured,
                    CreatedAt = p.CreatedAt,
                    Slug = p.Slug,
                    AuthorId = p.UserId,
                    AuthorName = p.Author != null ? $"{p.Author.FirstName} {p.Author.LastName}" : "Unknown Author",
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                    Tags = p.Tags.Select(t => t.Name).ToList()
                })
                .ToListAsync();

            var response = new PaginatedResponse<PostResponseDto>
            {
                Items = posts,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalPosts,
                ItemsPerPage = PostsPerPage
            };

            return Ok(response);
        }
    }
}