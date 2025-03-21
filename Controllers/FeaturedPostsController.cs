using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceInfo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturedPostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string FeaturedPostsCacheKey = "featured_posts";
        private readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public FeaturedPostsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/FeaturedPosts
        [HttpGet]
        public async Task<ActionResult<List<PostResponseDto>>> GetFeaturedPosts()
        {
            if (_cache.TryGetValue(FeaturedPostsCacheKey, out List<PostResponseDto> cachedPosts))
            {
                return Ok(cachedPosts);
            }

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => p.IsFeatured && !p.IsDeleted)
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

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration);
            _cache.Set(FeaturedPostsCacheKey, posts, cacheOptions);

            return Ok(posts);
        }

        // Optional: Force refresh endpoint
        [HttpGet("refresh")]
        public async Task<ActionResult<List<PostResponseDto>>> RefreshFeaturedPosts()
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => p.IsFeatured && !p.IsDeleted)
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

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration);
            _cache.Set(FeaturedPostsCacheKey, posts, cacheOptions);

            return Ok(posts);
        }
    }
}