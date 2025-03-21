using VoiceInfo.DTOs;

namespace VoiceInfo.IService
{
    public interface ICategory
    {

        Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto);
        Task<CategoryResponseDto> UpdateCategoryAsync(int categoryId, CategoryCreateDto categoryCreateDto);
        Task<CategoryResponseDto> GetCategoryByIdAsync(int categoryId);
        Task<List<CategoryResponseDto>> GetAllCategoriesAsync();
        Task<bool> DeleteCategoryAsync(int categoryId);
        Task<PaginatedResponse<PostResponseDto>> GetPostsByCategoryAsync(string categoryName, int pageNumber = 1, int pageSize = 15);
    }
}
