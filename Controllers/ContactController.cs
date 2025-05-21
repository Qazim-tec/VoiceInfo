using Microsoft.AspNetCore.Mvc;
using VoiceInfo.IService;

namespace VoiceInfoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // POST: api/contact
        [HttpPost]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormDto form)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Prepare HTML email body
            var htmlBody = $"<h3>New Contact Form Submission</h3>" +
                           $"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(form.Name)}</p>" +
                           $"<p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(form.Email)}</p>" +
                           $"<p><strong>Message:</strong> {System.Net.WebUtility.HtmlEncode(form.Message)}</p>" +
                           $"<p><strong>Submitted:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>";

            // Send email to admin
            await _emailService.SendEmailAsync("voiceinfos01@gmail.com", "New Contact Form Submission", htmlBody, "Contact Form Submission");

            return Ok(new { message = "Thank you for contacting us!" });
        }
    }

    public class ContactFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}