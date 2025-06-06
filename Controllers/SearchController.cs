﻿using Microsoft.AspNetCore.Mvc;
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
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const int PostsPerPage = 5;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Search?query=example&page=1
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<PostResponseDto>>> SearchPosts(
            [FromQuery] string query,
            [FromQuery] int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty.");
            if (page < 1) page = 1;

            var searchWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var totalPosts = await _context.Posts
                .CountAsync(p => !p.IsDeleted && searchWords.All(w => p.Title.ToLower().Contains(w)));

            var totalPages = (int)Math.Ceiling(totalPosts / (double)PostsPerPage);

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => !p.IsDeleted && searchWords.All(w => p.Title.ToLower().Contains(w)))
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