using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VoiceInfo.IService;
using VoiceInfo.DTOs;
using VoiceInfo.Services;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShareController> _logger;
        private const string FRONTEND_BASE_URL = "https://www.voiceinfos.com";
        private const string DEFAULT_IMAGE_URL = "https://www.voiceinfos.com/INFOS_LOGO%5B1%5D.png";

        public ShareController(IPostService postService, ILogger<ShareController> logger)
        {
            _postService = postService;
            _httpClient = new HttpClient();
            _logger = logger;
        }

        [HttpGet("generate-share-links/{postId}")]
        public async Task<IActionResult> GenerateShareLinks(int postId)
        {
            _logger.LogInformation("GenerateShareLinks called for postId: {PostId}, User-Agent: {UserAgent}", postId, Request.Headers["User-Agent"]);
            try
            {
                var post = await _postService.GetPostByIdAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning("Post not found for postId: {PostId}", postId);
                    return NotFound(new { error = "Post not found" });
                }

                var shareUrl = $"{FRONTEND_BASE_URL}/share/{Uri.EscapeDataString(post.Slug)}?cb={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                var excerpt = GetShareDescription(post);

                var shareLinks = new
                {
                    WhatsApp = $"https://api.whatsapp.com/send?text={Uri.EscapeDataString($"*{post.Title}*\n{excerpt}\n{shareUrl}")}",
                    Facebook = $"https://www.facebook.com/sharer/sharer.php?u={Uri.EscapeDataString(shareUrl)}",
                    Twitter = $"https://twitter.com/intent/tweet?text={Uri.EscapeDataString($"{post.Title}\n{excerpt}")}&url={Uri.EscapeDataString(shareUrl)}",
                    LinkedIn = $"https://www.linkedin.com/shareArticle?mini=true&url={Uri.EscapeDataString(shareUrl)}&title={Uri.EscapeDataString(post.Title)}&summary={Uri.EscapeDataString(excerpt)}"
                };

                _logger.LogInformation("Share links generated for postId: {PostId}, slug: {Slug}", postId, post.Slug);
                return Ok(new { data = shareLinks, message = "Share links generated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate share links for postId: {PostId}", postId);
                return StatusCode(500, new { error = "Failed to generate share links", details = ex.Message });
            }
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> ShareBySlug(string slug)
        {
            _logger.LogInformation("ShareBySlug called for slug: {Slug}, User-Agent: {UserAgent}", slug, Request.Headers["User-Agent"]);
            try
            {
                // Enhanced crawler detection
                var userAgent = Request.Headers["User-Agent"].ToString().ToLower();
                bool isCrawler = userAgent.Contains("whatsapp") ||
                                 userAgent.Contains("facebookexternal") ||
                                 userAgent.Contains("twitterbot") ||
                                 userAgent.Contains("linkedinbot") ||
                                 userAgent.Contains("slackbot") ||
                                 userAgent.Contains("discordbot") ||
                                 userAgent.Contains("googlebot") ||
                                 userAgent.Contains("bingbot") ||
                                 userAgent.Contains("pinterest") ||
                                 userAgent.Contains("redditbot") ||
                                 userAgent.Contains("facebot") ||
                                 userAgent.Contains("facebookcatalog") ||
                                 string.IsNullOrEmpty(userAgent);

                if (!isCrawler)
                {
                    _logger.LogInformation("Non-crawler request for slug: {Slug}, redirecting to post page", slug);
                    var postPageUrl = $"{FRONTEND_BASE_URL}/post/{Uri.EscapeDataString(slug)}";
                    Response.Headers.Add("X-Response-Type", "Redirect");
                    return RedirectPermanent(postPageUrl); // 301 redirect to post page
                }

                // Fetch post by slug
                var post = await _postService.GetPostBySlugAsync(slug);
                if (post == null)
                {
                    _logger.LogWarning("Post not found for slug: {Slug}", slug);
                    Response.Headers.Add("X-Response-Type", "NotFound");
                    post = new PostResponseDto
                    {
                        Title = "Post | VoiceInfo",
                        Excerpt = "Discover the latest insights on VoiceInfo",
                        Content = "Discover the latest insights on VoiceInfo",
                        FeaturedImageUrl = DEFAULT_IMAGE_URL,
                        Slug = slug
                    };
                }

                _logger.LogInformation("Post fetched for slug: {Slug}, Title: {Title}, FeaturedImageUrl: {FeaturedImageUrl}", slug, post.Title, post.FeaturedImageUrl);

                // Prepare OG tag data
                var title = System.Net.WebUtility.HtmlEncode(post.Title ?? "Post | VoiceInfo");
                var excerpt = System.Net.WebUtility.HtmlEncode(GetShareDescription(post));
                var shareUrl = $"{FRONTEND_BASE_URL}/post/{Uri.EscapeDataString(slug)}"; // Use post page URL for og:url

                // Validate image URL
                string imageUrl = DEFAULT_IMAGE_URL;
                string imageType = "image/png"; // Default for INFOS_LOGO%5B1%5D.png
                if (!string.IsNullOrEmpty(post.FeaturedImageUrl))
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(post.FeaturedImageUrl, HttpCompletionOption.ResponseHeadersRead);
                        var contentType = response.Content.Headers.ContentType?.MediaType;
                        if (response.IsSuccessStatusCode && contentType?.StartsWith("image/") == true)
                        {
                            imageUrl = post.FeaturedImageUrl;
                            imageType = contentType;
                            _logger.LogInformation("Valid FeaturedImageUrl: {FeaturedImageUrl}, Content-Type: {ContentType}", post.FeaturedImageUrl, contentType);
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

                // Generate HTML with OG tags
                var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <meta name=""description"" content=""{excerpt}"">
    <meta property=""og:title"" content=""{title}"">
    <meta property=""og:description"" content=""{excerpt}"">
    <meta property=""og:image"" content=""{imageUrl}"">
    <meta property=""og:image:width"" content=""1200"">
    <meta property=""og:image:height"" content=""630"">
    <meta property=""og:image:type"" content=""{imageType}"">
    <meta property=""og:image:alt"" content=""Image for {title}"">
    <meta property=""og:url"" content=""{shareUrl}"">
    <meta property=""og:type"" content=""article"">
    <meta property=""og:site_name"" content=""VoiceInfo"">
    <meta name=""twitter:card"" content=""summary_large_image"">
    <meta name=""twitter:title"" content=""{title}"">
    <meta name=""twitter:description"" content=""{excerpt}"">
    <meta name=""twitter:image"" content=""{imageUrl}"">
    <meta name=""twitter:image:alt"" content=""Image for {title}"">
</head>
<body>
    <h1>{title}</h1>
    <p>{excerpt}</p>
    <img src=""{imageUrl}"" alt=""{title}"">
</body>
</html>";

                _logger.LogInformation("Serving OG HTML for slug: {Slug}, ImageUrl: {ImageUrl}, ImageType: {ImageType}", slug, imageUrl, imageType);
                Response.Headers.Add("X-Response-Type", "OG-HTML");
                return Content(html, "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate share page for slug: {Slug}", slug);
                Response.Headers.Add("X-Response-Type", "Error");
                return StatusCode(500, new { error = "Failed to generate share page", details = ex.Message });
            }
        }

        private string GetShareDescription(PostResponseDto post)
        {
            return !string.IsNullOrEmpty(post.Excerpt)
                ? post.Excerpt
                : (!string.IsNullOrEmpty(post.Content) ? post.Content.Substring(0, Math.Min(post.Content.Length, 160)) : "Discover the latest insights on VoiceInfos");
        }
    }
}