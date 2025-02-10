using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Services;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategory _categoryService;

        public CategoryController(ICategory categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")] // Only admins can create categories
        public async Task<IActionResult> CreateCategory(CategoryCreateDto categoryCreateDto)
        {
            var category = await _categoryService.CreateCategoryAsync(categoryCreateDto);
            return Ok(category);
        }

        [HttpPut("update/{categoryId}")]
        [Authorize(Roles = "Admin")] // Only admins can update categories
        public async Task<IActionResult> UpdateCategory(int categoryId, CategoryCreateDto categoryCreateDto)
        {
            var category = await _categoryService.UpdateCategoryAsync(categoryId, categoryCreateDto);
            return Ok(category);
        }

        [HttpGet("{categoryId}")]
        public async Task<IActionResult> GetCategory(int categoryId)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);
            return Ok(category);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpDelete("delete/{categoryId}")]
        [Authorize(Roles = "Admin")] // Only admins can delete categories
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var result = await _categoryService.DeleteCategoryAsync(categoryId);
            return Ok(result);
        }
    }
}