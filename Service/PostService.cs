using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Models;

namespace VoiceInfo.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PostService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<PostResponseDto> CreatePostAsync(PostCreateDto postCreateDto, string userId)
        {
            var post = new Post
            {
                Title = postCreateDto.Title,
                Content = postCreateDto.Content,
                Excerpt = postCreateDto.Excerpt,
                UserId = userId,
                CategoryId = postCreateDto.CategoryId
            };

            // Handle image upload
            if (postCreateDto.FeaturedImage != null && postCreateDto.FeaturedImage.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await postCreateDto.FeaturedImage.CopyToAsync(memoryStream);
                    post.FeaturedImage = memoryStream.ToArray();
                }
            }

            post.GenerateSlug();

            if (postCreateDto.Tags != null && postCreateDto.Tags.Any())
            {
                post.Tags = await GetOrCreateTags(postCreateDto.Tags);
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Excerpt = post.Excerpt,
                FeaturedImage = post.FeaturedImage != null ? Convert.ToBase64String(post.FeaturedImage) : null,
                CreatedAt = post.CreatedAt,
                AuthorId = post.UserId,
                Slug = post.Slug
            };
        }

        public async Task<PostResponseDto> UpdatePostAsync(int postId, PostUpdateDto postUpdateDto, string userId)
        {
            var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null || post.UserId != userId)
                throw new KeyNotFoundException("Post not found or unauthorized.");

            post.Title = postUpdateDto.Title;
            post.Content = postUpdateDto.Content;
            post.Excerpt = postUpdateDto.Excerpt;
            post.CategoryId = postUpdateDto.CategoryId;

            if (postUpdateDto.FeaturedImage != null && postUpdateDto.FeaturedImage.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await postUpdateDto.FeaturedImage.CopyToAsync(memoryStream);
                    post.FeaturedImage = memoryStream.ToArray();
                }
            }

            post.GenerateSlug();

            if (postUpdateDto.Tags != null)
            {
                post.Tags = await GetOrCreateTags(postUpdateDto.Tags);
            }

            await _context.SaveChangesAsync();

            return new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Excerpt = post.Excerpt,
                FeaturedImage = post.FeaturedImage != null ? Convert.ToBase64String(post.FeaturedImage) : null,
                CreatedAt = post.CreatedAt,
                AuthorId = post.UserId,
                Slug = post.Slug
            };
        }

        public async Task<PostResponseDto> GetPostByIdAsync(int postId)
        {
            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .ThenInclude(c => c.Commenter)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            return new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Excerpt = post.Excerpt,
                FeaturedImage = post.FeaturedImage != null ? Convert.ToBase64String(post.FeaturedImage) : null,
                Views = post.Views,
                IsFeatured = post.IsFeatured,
                CreatedAt = post.CreatedAt,
                Slug = post.Slug,
                AuthorId = post.UserId,
                AuthorName = post.Author != null ? $"{post.Author.FirstName} {post.Author.LastName}" : "Unknown Author",
                CategoryId = post.CategoryId,
                CategoryName = post.Category != null ? post.Category.Name : "Uncategorized",
                Tags = post.Tags.Select(t => t.Name).ToList(),
                Comments = post.Comments
                    .Where(c => !c.IsDeleted)
                    .Select(c => new CommentResponseDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UserId = c.UserId,
                        UserName = c.Commenter != null ? $"{c.Commenter.FirstName} {c.Commenter.LastName}" : "Unknown User",
                        ParentCommentId = c.ParentCommentId,
                        Replies = c.Replies
                            .Where(r => !r.IsDeleted)
                            .Select(r => new CommentResponseDto
                            {
                                Id = r.Id,
                                Content = r.Content,
                                CreatedAt = r.CreatedAt,
                                UserId = r.UserId,
                                UserName = r.Commenter != null ? $"{r.Commenter.FirstName} {r.Commenter.LastName}" : "Unknown User",
                                ParentCommentId = r.ParentCommentId
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        public async Task<List<PostResponseDto>> GetAllPostsAsync()
        {
            return await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => !p.IsDeleted)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Excerpt = p.Excerpt,
                    FeaturedImage = p.FeaturedImage != null ? Convert.ToBase64String(p.FeaturedImage) : null,
                    Views = p.Views,
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
        }

        public async Task<bool> DeletePostAsync(int postId, string userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return false;

            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isAdmin && post.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this post.");

            post.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FeaturePostAsync(int postId, bool isFeatured)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            post.IsFeatured = isFeatured;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<List<Tag>> GetOrCreateTags(List<string> tagNames)
        {
            var existingTags = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
            var newTags = tagNames.Except(existingTags.Select(t => t.Name))
                                  .Select(t => new Tag { Name = t }).ToList();

            _context.Tags.AddRange(newTags);
            await _context.SaveChangesAsync();

            return existingTags.Concat(newTags).ToList();
        }
    }
}