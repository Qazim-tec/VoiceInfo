using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace VoiceInfo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LatestNewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string LatestNewsCacheKey = "latest_news_all"; // Single key for latest news
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

            // Try to get the full list of latest news from cache
            if (!_cache.TryGetValue(LatestNewsCacheKey, out List<PostResponseDto> allLatestNews))
            {
                // Fetch only posts where IsLatestNews is true and not deleted
                allLatestNews = await _context.Posts
                    .AsNoTracking()
                    .Include(p => p.Author)
                    .Include(p => p.Category)
                    .Include(p => p.Tags)
                    .Where(p => p.IsLatestNews && !p.IsDeleted) // Filter by IsLatestNews = true
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostResponseDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Content = p.Content,
                        Excerpt = p.Excerpt,
                        FeaturedImageUrl = p.FeaturedImageUrl,
                        Views = p.Views,
                        IsLatestNews = p.IsLatestNews, // Will always be true due to filter
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

                // Cache the filtered list
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(CacheDuration);
                _cache.Set(LatestNewsCacheKey, allLatestNews, cacheOptions);
            }

            // Paginate the cached list in memory
            var totalPosts = allLatestNews.Count;
            var totalPages = (int)Math.Ceiling(totalPosts / (double)PostsPerPage);

            var paginatedPosts = allLatestNews
                .Skip((page - 1) * PostsPerPage)
                .Take(PostsPerPage)
                .ToList();

            var response = new PaginatedResponse<PostResponseDto>
            {
                Items = paginatedPosts,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalPosts,
                ItemsPerPage = PostsPerPage
            };

            return Ok(response);
        }
    }
}