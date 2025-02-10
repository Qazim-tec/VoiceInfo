using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInfo.DTOs;

namespace VoiceInfo.Services
{
    public interface ICommentService
    {
        Task<CommentResponseDto> CreateCommentAsync(CommentCreateDto commentCreateDto, string userId);
        Task<CommentResponseDto> UpdateCommentAsync(int commentId, CommentUpdateDto commentUpdateDto, string userId);
        Task<CommentResponseDto> GetCommentByIdAsync(int commentId);
        Task<List<CommentResponseDto>> GetCommentsByPostIdAsync(int postId);
        Task<bool> DeleteCommentAsync(int commentId, string userId);
    }
}