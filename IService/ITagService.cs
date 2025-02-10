using VoiceInfo.DTOs;

namespace VoiceInfo.IService
{
    public interface ITagService
    {
        Task<TagResponseDto> CreateTagAsync(TagCreateDto tagCreateDto);
        Task<TagResponseDto> GetTagByIdAsync(int tagId);
        Task<List<TagResponseDto>> GetAllTagsAsync();
        Task<bool> DeleteTagAsync(int tagId);
    }
}
