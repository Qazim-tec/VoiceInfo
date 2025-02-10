using VoiceInfo.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VoiceInfo.IService
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto);
        Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync();
        Task<CategoryResponseDto> GetCategoryByIdAsync(int categoryId); // New method to get a category by ID
    }
}
