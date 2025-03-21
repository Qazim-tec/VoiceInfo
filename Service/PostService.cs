using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VoiceInfo.Data;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace VoiceInfo.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;
        private readonly ICategory _categoryService;

        public PostService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            Cloudinary cloudinary,
            IConfiguration configuration,
            ICategory categoryService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        }

        private async Task<string> UploadImageToCloudinary(IFormFile image)
        {
            if (image == null || image.Length == 0) return null;

            using var stream = image.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, stream),
                Transformation = new Transformation().Width(800).Height(600).Crop("limit")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<PostResponseDto> CreatePostAsync(PostCreateDto postCreateDto, string userId)
        {
            if (postCreateDto == null)
                throw new ArgumentNullException(nameof(postCreateDto), "Post creation data cannot be null.");
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            // Validate CategoryId if provided (handle nullable int)
            if (postCreateDto.CategoryId.HasValue && postCreateDto.CategoryId.Value > 0)
            {
                var category = await _categoryService.GetCategoryByIdAsync(postCreateDto.CategoryId.Value);
                if (category == null)
                    throw new ArgumentException($"Category with ID {postCreateDto.CategoryId.Value} does not exist or is deleted.");
            }

            var post = new Post
            {
                Title = postCreateDto.Title,
                Content = postCreateDto.Content,
                Excerpt = postCreateDto.Excerpt,
                UserId = userId,
                CategoryId = postCreateDto.CategoryId,
                IsFeatured = false,
                IsLatestNews = false,
                CreatedAt = DateTime.UtcNow
            };

            post.FeaturedImageUrl = await UploadImageToCloudinary(postCreateDto.FeaturedImage);
            post.GenerateSlug();

            if (postCreateDto.Tags != null && postCreateDto.Tags.Any())
            {
                post.Tags = await GetOrCreateTags(postCreateDto.Tags);
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Eagerly load related data
            await _context.Entry(post).Reference(p => p.Author).LoadAsync();
            if (post.CategoryId.HasValue)
                await _context.Entry(post).Reference(p => p.Category).LoadAsync();

            return await GetPostResponseDto(post);
        }

        public async Task<PostResponseDto> UpdatePostAsync(int postId, PostUpdateDto postUpdateDto, string userId)
        {
            var post = await _context.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null || post.UserId != userId)
                throw new KeyNotFoundException("Post not found or unauthorized.");

            // Validate CategoryId if provided
            if (postUpdateDto.CategoryId.HasValue && postUpdateDto.CategoryId.Value > 0)
            {
                var category = await _categoryService.GetCategoryByIdAsync(postUpdateDto.CategoryId.Value);
                if (category == null)
                    throw new ArgumentException($"Category with ID {postUpdateDto.CategoryId.Value} does not exist or is deleted.");
            }

            post.Title = postUpdateDto.Title;
            post.Content = postUpdateDto.Content;
            post.Excerpt = postUpdateDto.Excerpt;
            post.CategoryId = postUpdateDto.CategoryId;

            if (postUpdateDto.FeaturedImage != null && postUpdateDto.FeaturedImage.Length > 0)
            {
                post.FeaturedImageUrl = await UploadImageToCloudinary(postUpdateDto.FeaturedImage);
            }

            post.GenerateSlug();

            if (postUpdateDto.Tags != null)
            {
                post.Tags = await GetOrCreateTags(postUpdateDto.Tags);
            }

            await _context.SaveChangesAsync();

            // Reload related data
            await _context.Entry(post).Reference(p => p.Author).LoadAsync();
            if (post.CategoryId.HasValue)
                await _context.Entry(post).Reference(p => p.Category).LoadAsync();

            return await GetPostResponseDto(post);
        }

        public async Task<PostResponseDto> GetPostByIdAsync(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Include(p => p.Comments).ThenInclude(c => c.Commenter)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            post.Views += 1;
            await _context.SaveChangesAsync();

            return await GetPostResponseDto(post);
        }

        public async Task<PostResponseDto> GetPostBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug cannot be empty or null.", nameof(slug));

            slug = slug.Trim().ToLower();

            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Include(p => p.Comments).ThenInclude(c => c.Commenter)
                .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug && !p.IsDeleted);

            if (post == null)
                throw new KeyNotFoundException($"Post with slug '{slug}' not found.");

            post.Views += 1;
            await _context.SaveChangesAsync();

            return await GetPostResponseDto(post);
        }

        public async Task<List<PostResponseDto>> GetAllPostsAsync()
        {
            var posts = await _context.Posts
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
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    Views = p.Views,
                    IsLatestNews = p.IsLatestNews,
                    IsFeatured = p.IsFeatured,
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

            return posts;
        }

        public async Task<bool> DeletePostAsync(int postId, string userId)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null) return false;

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (!isAdmin && post.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this post.");

            post.IsDeleted = true;
            post.CategoryId = null;
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

        public async Task<bool> SetLatestNewsAsync(int postId, bool isLatestNews)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            post.IsLatestNews = isLatestNews;
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<PostResponseDto> GetPostResponseDto(Post post)
        {
            var allComments = post.Comments?.Where(c => !c.IsDeleted).ToList() ?? new List<Comment>();
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

            string authorName = "Unknown Author";
            if (post.Author != null)
            {
                authorName = $"{post.Author.FirstName} {post.Author.LastName}";
            }
            else
            {
                var user = await _userManager.FindByIdAsync(post.UserId);
                if (user != null)
                {
                    authorName = $"{user.FirstName} {user.LastName}";
                }
            }

            string categoryName = "Uncategorized";
            if (post.Category != null)
            {
                categoryName = post.Category.Name;
            }
            else if (post.CategoryId.HasValue && post.CategoryId.Value > 0)
            {
                var category = await _categoryService.GetCategoryByIdAsync(post.CategoryId.Value);
                if (category != null)
                {
                    categoryName = category.Name;
                }
            }

            return new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Excerpt = post.Excerpt,
                FeaturedImageUrl = post.FeaturedImageUrl,
                Views = post.Views,
                IsFeatured = post.IsFeatured,
                IsLatestNews = post.IsLatestNews,
                CreatedAt = post.CreatedAt,
                Slug = post.Slug,
                AuthorId = post.UserId,
                AuthorName = authorName,
                CategoryId = post.CategoryId ?? 0,
                CategoryName = categoryName,
                Tags = post.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                CommentsCount = allComments.Count,
                Comments = rootComments
            };
        }

        private async Task<List<Tag>> GetOrCreateTags(List<string> tagNames)
        {
            var existingTags = await _context.Tags
                .Where(t => tagNames.Contains(t.Name))
                .ToListAsync();

            var newTags = tagNames
                .Except(existingTags.Select(t => t.Name))
                .Select(t => new Tag { Name = t })
                .ToList();

            if (newTags.Any())
            {
                _context.Tags.AddRange(newTags);
                await _context.SaveChangesAsync();
            }

            return existingTags.Concat(newTags).ToList();
        }

        public async Task<PaginatedResponse<PostLightDto>> GetAllPostsLightAsync(int pageNumber = 1, int pageSize = 15)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 15;

            var totalPosts = await _context.Posts
                .Where(p => !p.IsDeleted)
                .CountAsync();

            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostLightDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.Author != null ? $"{p.Author.FirstName} {p.Author.LastName}" : "Unknown Author",
                    IsFeatured = p.IsFeatured,
                    IsLatestNews = p.IsLatestNews
                })
                .ToListAsync();

            var result = new PaginatedResponse<PostLightDto>
            {
                Items = posts,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                TotalItems = totalPosts,
                ItemsPerPage = pageSize
            };

            return result;
        }
    }
}