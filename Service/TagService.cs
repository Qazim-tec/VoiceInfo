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
    public class TagService : ITagService
    {
        private readonly ApplicationDbContext _context;

        public TagService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TagResponseDto> CreateTagAsync(TagCreateDto tagCreateDto)
        {
            var tag = new Tag
            {
                Name = tagCreateDto.Name
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return new TagResponseDto
            {
                Id = tag.Id,
                Name = tag.Name,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<TagResponseDto> GetTagByIdAsync(int tagId)
        {
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null)
                throw new System.Exception("Tag not found.");

            return new TagResponseDto
            {
                Id = tag.Id,
                Name = tag.Name,
                CreatedAt = tag.CreatedAt
            };
        }

       

        public async Task<List<TagResponseDto>> GetAllTagsAsync()
        {
            var tags = await _context.Tags
                .Select(t => new TagResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return tags;
        }

        public async Task<bool> DeleteTagAsync(int tagId)
        {
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null)
                throw new System.Exception("Tag not found.");

            tag.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}