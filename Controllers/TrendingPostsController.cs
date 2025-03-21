using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    public class TrendingPostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string TrendingPostsCacheKey = "trending_posts";
        private readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
        private const int PostsPerPage = 4;

        public TrendingPostsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/TrendingPosts
        [HttpGet]
        public async Task<ActionResult<PostResponseDto[]>> GetTrendingPosts()
        {
            if (_cache.TryGetValue(TrendingPostsCacheKey, out PostResponseDto[] cachedPosts))
            {
                return Ok(cachedPosts);
            }

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Comments) // Still needed to count comments
                .Where(p => !p.IsDeleted)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CreatedAt = p.CreatedAt,
                    Views = p.Views,
                    CommentsCount = p.Comments.Count(c => !c.IsDeleted), // Count non-deleted comments
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    AuthorName = p.Author != null ? $"{p.Author.FirstName} {p.Author.LastName}" : "Unknown Author"
                })
                .ToListAsync();

            var trendingPosts = posts
                .OrderByDescending(p => (p.CommentsCount * 2) + p.Views)
                .ThenByDescending(p => p.CreatedAt)
                .Take(PostsPerPage)
                .ToArray();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration);
            _cache.Set(TrendingPostsCacheKey, trendingPosts, cacheOptions);

            return Ok(trendingPosts);
        }
    }
}