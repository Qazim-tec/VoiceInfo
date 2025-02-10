using System.ComponentModel.DataAnnotations;

namespace VoiceInfo.DTOs
{
    public class PaginationDto
    {
        [Required]
        public int PageNumber { get; set; } = 1;

        [Required]
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }

    public class ApiResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public int StatusCode { get; set; }

        public ApiResponseDto(bool success, string message, object data = null, int statusCode = 200)
        {
            Success = success;
            Message = message;
            Data = data;
            StatusCode = statusCode;
        }
    }
}