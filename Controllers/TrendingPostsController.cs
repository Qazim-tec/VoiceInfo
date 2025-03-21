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
    public class TrendingPostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const int PostsPerPage = 4;

        public TrendingPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TrendingPosts
        [HttpGet]
        public async Task<ActionResult<PostResponseDto[]>> GetTrendingPosts()
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .Where(p => !p.IsDeleted)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CreatedAt = p.CreatedAt,
                    Views = p.Views,
                    CommentsCount = p.Comments.Count(c => !c.IsDeleted),
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

            return Ok(trendingPosts);
        }
    }
}