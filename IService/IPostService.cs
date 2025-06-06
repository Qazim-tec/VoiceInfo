﻿using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInfo.DTOs;

namespace VoiceInfo.Services
{
    public interface IPostService
    {
        Task<PostResponseDto> CreatePostAsync(PostCreateDto postCreateDto, string userId);
        Task<PostResponseDto> UpdatePostAsync(int postId, PostUpdateDto postUpdateDto, string userId);
        Task<PostResponseDto> GetPostByIdAsync(int postId);
        Task<List<PostResponseDto>> GetAllPostsAsync();
        Task<bool> DeletePostAsync(int postId, string userId);
        Task<bool> FeaturePostAsync(int postId, bool isFeatured);
        Task<bool> SetLatestNewsAsync(int postId, bool isLatestNews);
        Task<PostResponseDto> GetPostBySlugAsync(string slug); // Added this method
        Task<PaginatedResponse<PostLightDto>> GetAllPostsLightAsync(int pageNumber = 1, int pageSize = 15);
        Task<bool> LikePostAsync(int postId, string userId);
        Task<bool> UnlikePostAsync(int postId, string userId);
    }
}