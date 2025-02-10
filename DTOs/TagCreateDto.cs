using System.ComponentModel.DataAnnotations;

namespace VoiceInfo.DTOs
{
    public class TagCreateDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; }
    }

    public class TagResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
