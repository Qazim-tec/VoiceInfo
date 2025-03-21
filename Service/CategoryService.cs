using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;
        private const string CategoriesCacheKey = "categories_all";
        private readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10); // Cache for 10 minutes

        public CategoryService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto)
        {
            var category = new Category
            {
                Name = categoryCreateDto.Name
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _cache.Remove(CategoriesCacheKey); // Invalidate cache on create

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                CreatedAt = category.CreatedAt
            };
        }

        public async Task<CategoryResponseDto> UpdateCategoryAsync(int categoryId, CategoryCreateDto categoryCreateDto)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                throw new Exception("Category not found.");

            category.Name = categoryCreateDto.Name;
            await _context.SaveChangesAsync();

            _cache.Remove(CategoriesCacheKey); // Invalidate cache on update

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                CreatedAt = category.CreatedAt
            };
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
            if (_cache.TryGetValue(CategoriesCacheKey, out List<CategoryResponseDto> cachedCategories))
            {
                return cachedCategories;
            }

            var categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration);
            _cache.Set(CategoriesCacheKey, categories, cacheOptions);

            return categories;
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                throw new Exception("Category not found.");

            category.IsDeleted = true;
            await _context.SaveChangesAsync();

            _cache.Remove(CategoriesCacheKey); // Invalidate cache on delete

            return true;
        }
    }
}