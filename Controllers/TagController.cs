using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VoiceInfo.DTOs;
using VoiceInfo.IService;
using VoiceInfo.Services;

namespace VoiceInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")] // Only admins can create tags
        public async Task<IActionResult> CreateTag(TagCreateDto tagCreateDto)
        {
            var tag = await _tagService.CreateTagAsync(tagCreateDto);
            return Ok(tag);
        }

        [HttpGet("{tagId}")]
        public async Task<IActionResult> GetTag(int tagId)
        {
            var tag = await _tagService.GetTagByIdAsync(tagId);
            return Ok(tag);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }

        [HttpDelete("delete/{tagId}")]
        [Authorize(Roles = "Admin")] // Only admins can delete tags
        public async Task<IActionResult> DeleteTag(int tagId)
        {
            var result = await _tagService.DeleteTagAsync(tagId);
            return Ok(result);
        }
    }
}