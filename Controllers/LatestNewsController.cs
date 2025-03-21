using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VoiceInfo.Data;
using VoiceInfo.DTOs; // Ensure this is included
using VoiceInfo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceInfo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LatestNewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string LatestNewsCacheKeyPrefix = "latest_news_page_";
        private readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private const int PostsPerPage = 15;

        public LatestNewsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/LatestNews?page=1
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<PostResponseDto>>> GetLatestNews([FromQuery] int page = 1)
        {
            if (page < 1) page = 1;

            string cacheKey = $"{LatestNewsCacheKeyPrefix}{page}";
            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<PostResponseDto> cachedResponse))
            {
                return Ok(cachedResponse);
            }

            var totalPosts = await _context.Posts
                .CountAsync(p => p.IsLatestNews && !p.IsDeleted);

            var totalPages = (int)Math.Ceiling(totalPosts / (double)PostsPerPage);

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => p.IsLatestNews && !p.IsDeleted)
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

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration);
            _cache.Set(cacheKey, response, cacheOptions);

            return Ok(response);
        }
    }
}