using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private const int PostsPerPage = 15;

        public LatestNewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LatestNews?page=1
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<PostResponseDto>>> GetLatestNews([FromQuery] int page = 1)
        {
            if (page < 1) page = 1;

            var allLatestNews = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => p.IsLatestNews && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
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