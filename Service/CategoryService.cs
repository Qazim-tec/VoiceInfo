using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Models;

namespace VoiceInfo.Services
{
    public class CategoryService : ICategory
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto)
        {
            if (categoryCreateDto == null || string.IsNullOrWhiteSpace(categoryCreateDto.Name))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryCreateDto));

            var category = new Category { Name = categoryCreateDto.Name };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return new CategoryResponseDto { Id = category.Id, Name = category.Name, CreatedAt = category.CreatedAt };
        }

        public async Task<CategoryResponseDto> UpdateCategoryAsync(int categoryId, CategoryCreateDto categoryCreateDto)
        {
            if (categoryCreateDto == null || string.IsNullOrWhiteSpace(categoryCreateDto.Name))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryCreateDto));

            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                throw new Exception("Category not found.");

            category.Name = categoryCreateDto.Name;
            await _context.SaveChangesAsync();
            return new CategoryResponseDto { Id = category.Id, Name = category.Name, CreatedAt = category.CreatedAt };
        }

        public async Task<CategoryResponseDto> GetCategoryByIdAsync(int categoryId)
        {
            var category = await _context.Categories
                .Where(c => c.Id == categoryId && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (category == null)
                throw new Exception("Category not found or has been deleted.");

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                CreatedAt = category.CreatedAt
            };
        }

        public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return categories;
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                throw new Exception("Category not found.");

            category.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PaginatedResponse<PostResponseDto>> GetPostsByCategoryAsync(string categoryName, int pageNumber = 1, int pageSize = 15)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 15;

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower() && !c.IsDeleted);

            if (category == null)
                throw new KeyNotFoundException($"Category '{categoryName}' not found.");

            var totalPosts = await _context.Posts
                .Where(p => p.CategoryId == category.Id && !p.IsDeleted)
                .CountAsync();

            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.CategoryId == category.Id && !p.IsDeleted)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Excerpt = p.Excerpt,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    Views = p.Views,
                    IsFeatured = p.IsFeatured,
                    IsLatestNews = p.IsLatestNews,
                    CreatedAt = p.CreatedAt,
                    Slug = p.Slug,
                    AuthorId = p.UserId,
                    AuthorName = p.Author != null ? $"{p.Author.FirstName} {p.Author.LastName}" : "Unknown Author",
                    CategoryId = p.CategoryId ?? 0,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                    Tags = p.Tags.Select(t => t.Name).ToList(),
                    CommentsCount = p.Comments.Count(c => !c.IsDeleted)
                })
                .ToListAsync();

            var result = new PaginatedResponse<PostResponseDto>
            {
                Items = posts,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                TotalItems = totalPosts,
                ItemsPerPage = pageSize
            };

            return result;
        }

        public async Task<List<CategoryWithPostsDto>> GetCategoriesWithTopPostsAsync(int postsPerCategory = 3)
        {
            var categoriesWithPosts = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategoryWithPostsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Posts = c.Posts
                        .Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(postsPerCategory)
                        .Select(p => new PostResponseDto
                        {
                            Id = p.Id,
                            Title = p.Title,
                            Slug = p.Slug,
                            CreatedAt = p.CreatedAt,
                            CategoryId = p.CategoryId ?? 0,
                            CategoryName = c.Name
                        })
                        .ToList()
                })
                .Where(c => c.Posts.Any())
                .ToListAsync();

            return categoriesWithPosts;
        }
    }
}