using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Models;

namespace VoiceInfo.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommentService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<CommentResponseDto> CreateCommentAsync(CommentCreateDto commentCreateDto, string userId)
        {
            var comment = new Comment
            {
                Content = commentCreateDto.Content,
                UserId = userId,
                PostId = commentCreateDto.PostId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId
            };
        }

        public async Task<CommentResponseDto> UpdateCommentAsync(int commentId, CommentUpdateDto commentUpdateDto, string userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null || comment.UserId != userId)
                throw new System.Exception("Comment not found or unauthorized.");

            comment.Content = commentUpdateDto.Content;
            await _context.SaveChangesAsync();

            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId
            };
        }

        public async Task<CommentResponseDto> GetCommentByIdAsync(int commentId)
        {
            var comment = await _context.Comments
                .Include(c => c.Commenter)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                throw new System.Exception("Comment not found.");

            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                UserName = comment.Commenter.FirstName + " " + comment.Commenter.LastName
            };
        }

        public async Task<List<CommentResponseDto>> GetCommentsByPostIdAsync(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.Commenter)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UserId = c.UserId,
                    UserName = c.Commenter.FirstName + " " + c.Commenter.LastName
                })
                .ToListAsync();

            return comments;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, string userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return false;

            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isAdmin && comment.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this comment.");

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}