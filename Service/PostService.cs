using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private const string FeaturedPostsCacheKey = "featured_posts";
        private const string LatestNewsCacheKeyPrefix = "latest_news_page_";
        private const string MyPostsCacheKeyPrefix = "my_posts_page_";
        private const string AllPostsCacheKey = "all_posts";
        private const string TrendingPostsCacheKey = "trending_posts";
        private const string PostCacheKeyPrefix = "post_";
        private const string PostSlugCacheKeyPrefix = "post_slug_";

        public PostService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            Cloudinary cloudinary,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _cloudinary = cloudinary;
            _cache = cache;
            _configuration = configuration;
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
            var post = new Post
            {
                Title = postCreateDto.Title,
                Content = postCreateDto.Content,
                Excerpt = postCreateDto.Excerpt,
                UserId = userId,
                CategoryId = postCreateDto.CategoryId,
                IsFeatured = false,
                IsLatestNews = false
            };

            post.FeaturedImageUrl = await UploadImageToCloudinary(postCreateDto.FeaturedImage);
            post.GenerateSlug();

            if (postCreateDto.Tags != null && postCreateDto.Tags.Any())
            {
                post.Tags = await GetOrCreateTags(postCreateDto.Tags);
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            _cache.Remove(AllPostsCacheKey);
            _cache.Remove(TrendingPostsCacheKey);
            if (post.IsFeatured) _cache.Remove(FeaturedPostsCacheKey);
            if (post.IsLatestNews) InvalidateLatestNewsCache();
            InvalidateMyPostsCache(userId);
            _cache.Remove($"{PostCacheKeyPrefix}{post.Id}");
            _cache.Remove($"{PostSlugCacheKeyPrefix}{post.Slug}");

            return await GetPostResponseDto(post);
        }

        public async Task<PostResponseDto> UpdatePostAsync(int postId, PostUpdateDto postUpdateDto, string userId)
        {
            var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null || post.UserId != userId)
                throw new KeyNotFoundException("Post not found or unauthorized.");

            bool wasFeatured = post.IsFeatured;
            bool wasLatestNews = post.IsLatestNews;
            string oldSlug = post.Slug;

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

            _cache.Remove($"{PostCacheKeyPrefix}{postId}");
            _cache.Remove($"{PostSlugCacheKeyPrefix}{oldSlug}");
            _cache.Remove($"{PostSlugCacheKeyPrefix}{post.Slug}");
            _cache.Remove(AllPostsCacheKey);
            _cache.Remove(TrendingPostsCacheKey);
            if (wasFeatured || post.IsFeatured) _cache.Remove(FeaturedPostsCacheKey);
            if (wasLatestNews || post.IsLatestNews) InvalidateLatestNewsCache();
            InvalidateMyPostsCache(userId);

            return await GetPostResponseDto(post);
        }

        public async Task<PostResponseDto> GetPostByIdAsync(int postId)
        {
            string cacheKey = $"{PostCacheKeyPrefix}{postId}";
            if (_cache.TryGetValue(cacheKey, out PostResponseDto cachedPost))
            {
                return cachedPost;
            }

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

            var postDto = await GetPostResponseDto(post);

            var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(cacheKey, postDto, cacheOptions);
            _cache.Set($"{PostSlugCacheKeyPrefix}{post.Slug}", postDto, cacheOptions);

            return postDto;
        }

        public async Task<PostResponseDto> GetPostBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug cannot be empty or null.");

            // Normalize slug to avoid case sensitivity issues
            slug = slug.Trim().ToLower();

            string cacheKey = $"{PostSlugCacheKeyPrefix}{slug}";
            if (_cache.TryGetValue(cacheKey, out PostResponseDto cachedPost))
            {
                return cachedPost;
            }

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

            var postDto = await GetPostResponseDto(post);

            var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(cacheKey, postDto, cacheOptions);
            _cache.Set($"{PostCacheKeyPrefix}{post.Id}", postDto, cacheOptions);

            return postDto;
        }

        public async Task<List<PostResponseDto>> GetAllPostsAsync()
        {
            if (_cache.TryGetValue(AllPostsCacheKey, out List<PostResponseDto> cachedPosts))
            {
                return cachedPosts;
            }

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
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                    Tags = p.Tags.Select(t => t.Name).ToList(),
                    CommentsCount = p.Comments.Count(c => !c.IsDeleted)
                })
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(AllPostsCacheKey, posts, cacheOptions);

            return posts;
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

            _cache.Remove($"{PostCacheKeyPrefix}{postId}");
            _cache.Remove($"{PostSlugCacheKeyPrefix}{post.Slug}");
            _cache.Remove(AllPostsCacheKey);
            _cache.Remove(TrendingPostsCacheKey);
            if (post.IsFeatured) _cache.Remove(FeaturedPostsCacheKey);
            if (post.IsLatestNews) InvalidateLatestNewsCache();
            InvalidateMyPostsCache(userId);

            return true;
        }

        public async Task<bool> FeaturePostAsync(int postId, bool isFeatured)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            post.IsFeatured = isFeatured;
            await _context.SaveChangesAsync();

            _cache.Remove($"{PostCacheKeyPrefix}{postId}");
            _cache.Remove($"{PostSlugCacheKeyPrefix}{post.Slug}");
            _cache.Remove(AllPostsCacheKey);
            _cache.Remove(FeaturedPostsCacheKey);
            _cache.Remove(TrendingPostsCacheKey);
            InvalidateMyPostsCache(post.UserId);

            return true;
        }

        public async Task<bool> SetLatestNewsAsync(int postId, bool isLatestNews)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            post.IsLatestNews = isLatestNews;
            await _context.SaveChangesAsync();

            _cache.Remove($"{PostCacheKeyPrefix}{postId}");
            _cache.Remove($"{PostSlugCacheKeyPrefix}{post.Slug}");
            _cache.Remove(AllPostsCacheKey);
            _cache.Remove(TrendingPostsCacheKey);
            if (isLatestNews || post.IsLatestNews) InvalidateLatestNewsCache();
            InvalidateMyPostsCache(post.UserId);

            return true;
        }

        private async Task<PostResponseDto> GetPostResponseDto(Post post)
        {
            var allComments = post.Comments.Where(c => !c.IsDeleted).ToList();
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
                AuthorName = post.Author != null ? $"{post.Author.FirstName} {post.Author.LastName}" : "Unknown Author",
                CategoryId = post.CategoryId,
                CategoryName = post.Category != null ? post.Category.Name : "Uncategorized",
                Tags = post.Tags.Select(t => t.Name).ToList(),
                CommentsCount = allComments.Count,
                Comments = rootComments
            };
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

        private void InvalidateLatestNewsCache()
        {
            for (int i = 1; i <= 100; i++)
            {
                _cache.Remove($"{LatestNewsCacheKeyPrefix}{i}");
            }
        }

        private void InvalidateMyPostsCache(string userId)
        {
            for (int i = 1; i <= 100; i++)
            {
                _cache.Remove($"{MyPostsCacheKeyPrefix}{userId}_{i}");
            }
        }
    }
}