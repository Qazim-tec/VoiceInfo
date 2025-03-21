using Microsoft.AspNetCore.Identity;
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
                PostId = commentCreateDto.PostId,
                ParentCommentId = commentCreateDto.ParentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);
            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User",
                ParentCommentId = comment.ParentCommentId,
                Replies = new List<CommentResponseDto>()
            };
        }

        public async Task<CommentResponseDto> UpdateCommentAsync(int commentId, CommentUpdateDto commentUpdateDto, string userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null || comment.UserId != userId)
                throw new Exception("Comment not found or unauthorized.");

            comment.Content = commentUpdateDto.Content;
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);
            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User",
                ParentCommentId = comment.ParentCommentId,
                Replies = new List<CommentResponseDto>()
            };
        }

        public async Task<CommentResponseDto> GetCommentByIdAsync(int commentId)
        {
            var comment = await _context.Comments
                .Include(c => c.Commenter)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
                throw new Exception("Comment not found.");

            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                UserName = comment.Commenter != null ? $"{comment.Commenter.FirstName} {comment.Commenter.LastName}" : "Unknown User",
                ParentCommentId = comment.ParentCommentId,
                Replies = new List<CommentResponseDto>()
            };
        }

        public async Task<List<CommentResponseDto>> GetCommentsByPostIdAsync(int postId)
        {
            var allComments = await _context.Comments
                .Include(c => c.Commenter)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .ToListAsync();

            var commentDict = allComments.ToDictionary(
                c => c.Id,
                c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UserId = c.UserId,
                    UserName = c.Commenter != null ? $"{c.Commenter.FirstName} {c.Commenter.LastName}" : "Unknown User",
                    ParentCommentId = c.ParentCommentId,
                    Replies = new List<CommentResponseDto>()
                }
            );

            var rootComments = new List<CommentResponseDto>();
            foreach (var comment in commentDict.Values)
            {
                if (comment.ParentCommentId == null)
                {
                    rootComments.Add(comment);
                }
                else if (commentDict.ContainsKey(comment.ParentCommentId.Value))
                {
                    commentDict[comment.ParentCommentId.Value].Replies.Add(comment);
                }
            }

            return rootComments;
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