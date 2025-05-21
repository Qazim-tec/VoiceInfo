using System.Threading.Tasks;

namespace VoiceInfo.IService
{
    public interface IEmailService
    {
        Task SendVerificationOtpAsync(string toEmail, string otp);
        Task SendResetPasswordOtpAsync(string toEmail, string otp);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string logContext);
    }
}