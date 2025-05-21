using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VoiceInfo.IService;
using VoiceInfo.DTOs;
using VoiceInfo.Services;

namespace VoiceInfo.Middleware
{
    public class OpenGraphMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OpenGraphMiddleware> _logger;
        private readonly HttpClient _httpClient;

        public OpenGraphMiddleware(RequestDelegate next, ILogger<OpenGraphMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task InvokeAsync(HttpContext context, IPostService postService)
        {
            // Check if the request is for a blog post page (e.g., /post/slug)
            if (context.Request.Path.StartsWithSegments("/post") && context.Request.Path.Value.Split('/').Length > 2)
            {
                var slug = context.Request.Path.Value.Split('/').LastOrDefault();
                if (!string.IsNullOrEmpty(slug))
                {
                    try
                    {
                        var post = await postService.GetPostBySlugAsync(slug);
                        if (post != null)
                        {
                            _logger.LogInformation("Post FeaturedImageUrl for slug {Slug}: {FeaturedImageUrl}", slug, post.FeaturedImageUrl);
                            var ogTags = await GenerateOgTags(post, context.Request);
                            context.Items["OgTags"] = ogTags; // Store for view to render
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate OG tags for slug: {Slug}", slug);
                    }
                }
            }

            await _next(context);
        }

        private async Task<string> GenerateOgTags(PostResponseDto post, HttpRequest request)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var postUrl = $"{baseUrl}/post/{post.Slug}";
            var excerpt = !string.IsNullOrEmpty(post.Excerpt)
                ? post.Excerpt
                : (post.Content.Length > 100 ? post.Content.Substring(0, 100) + "..." : post.Content);

            // Escape HTML characters to prevent malformed HTML or XSS
            var title = System.Net.WebUtility.HtmlEncode(post.Title);
            excerpt = System.Net.WebUtility.HtmlEncode(excerpt);

            // Validate FeaturedImageUrl
            string imageUrl = "https://www.voiceinfos.com/INFOS_LOGO%5B1%5D.png"; // Default image
            if (!string.IsNullOrEmpty(post.FeaturedImageUrl))
            {
                try
                {
                    var response = await _httpClient.GetAsync(post.FeaturedImageUrl, HttpCompletionOption.ResponseHeadersRead);
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (response.IsSuccessStatusCode && contentType?.StartsWith("image/") == true)
                    {
                        imageUrl = post.FeaturedImageUrl;
                        _logger.LogInformation("Valid FeaturedImageUrl: {FeaturedImageUrl}", post.FeaturedImageUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid FeaturedImageUrl: Status {StatusCode}, Content-Type {ContentType}, URL {FeaturedImageUrl}",
                            response.StatusCode, contentType, post.FeaturedImageUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate FeaturedImageUrl: {FeaturedImageUrl}", post.FeaturedImageUrl);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"<meta property=\"og:title\" content=\"{title}\" />");
            sb.AppendLine($"<meta property=\"og:description\" content=\"{excerpt}\" />");
            sb.AppendLine($"<meta property=\"og:image\" content=\"{imageUrl}\" />");
            sb.AppendLine($"<meta property=\"og:url\" content=\"{postUrl}\" />");
            sb.AppendLine("<meta property=\"og:type\" content=\"article\" />");

            return sb.ToString();
        }
    }

    public static class OpenGraphMiddlewareExtensions
    {
        public static IApplicationBuilder UseOpenGraphMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OpenGraphMiddleware>();
        }
    }
}