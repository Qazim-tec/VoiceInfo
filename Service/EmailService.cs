using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using VoiceInfo.IService;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace VoiceInfo.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _emailFrom;
        private readonly string _emailPassword;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _smtpHost = configuration["EmailSettings:SmtpServer"] ?? throw new ArgumentNullException("SMTP server is not configured.");
            _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"], out int port) ? port : 465; // Default to 465 for SSL
            _emailFrom = configuration["EmailSettings:FromEmail"] ?? throw new ArgumentNullException("From email is not configured.");
            _emailPassword = configuration["EmailSettings:SmtpPassword"] ?? throw new ArgumentNullException("SMTP password is not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendVerificationOtpAsync(string toEmail, string otp)
        {
            var subject = "Welcome to VoiceInfos - Verify Your Email";
            var body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Welcome to VoiceInfos</title>
                    <style>
                        body {{
                            font-family: 'Helvetica Neue', Arial, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff;
                            border-radius: 10px;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                            overflow: hidden;
                        }}
                        .header {{
                            background-color: #007BFF;
                            color: #ffffff;
                            padding: 20px;
                            text-align: center;
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 26px;
                            font-weight: 500;
                        }}
                        .content {{
                            padding: 30px;
                            color: #333333;
                            line-height: 1.6;
                        }}
                        .otp {{
                            display: inline-block;
                            font-size: 32px;
                            font-weight: bold;
                            color: #007BFF;
                            background-color: #f1f1f1;
                            padding: 15px 25px;
                            border-radius: 8px;
                            margin: 20px 0;
                            letter-spacing: 2px;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            background-color: #007BFF;
                            color: #ffffff;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                            margin-top: 20px;
                        }}
                        .button:hover {{
                            background-color: #0056b3;
                        }}
                        .footer {{
                            background-color: #f4f4f4;
                            padding: 20px;
                            text-align: center;
                            font-size: 14px;
                            color: #666666;
                        }}
                        .footer a {{
                            color: #007BFF;
                            text-decoration: none;
                        }}
                        @media only screen and (max-width: 600px) {{
                            .container {{
                                margin: 10px;
                            }}
                            .content {{
                                padding: 20px;
                            }}
                            .otp {{
                                font-size: 28px;
                                padding: 10px 20px;
                            }}
                            .header h1 {{
                                font-size: 22px;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to VoiceInfos!</h1>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>Thank you for joining VoiceInfos, your platform to stay informed, share your voice, and connect with a vibrant community. To verify your email address and start exploring the latest news, creating posts, and commenting, please use the One-Time Password (OTP) below:</p>
                            <div class='otp'>{otp}</div>
                            <p>This OTP is valid for <strong>10 minutes</strong>. For your security, do not share this code with anyone.</p>
                            <p>Once verified, dive into VoiceInfos to read breaking news, share your thoughts, and engage with others in our community!</p>
                            <p>If you didn’t sign up for VoiceInfos, please ignore this email or contact our support team.</p>
                            <a href='mailto:support@voiceinfos.com' class='button'>Contact Support</a>
                        </div>
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} VoiceInfos. All rights reserved.</p>
                            <p><a href='https://www.voiceinfos.com'>Visit VoiceInfos</a> | <a href='mailto:support@voiceinfos.com'>Support</a></p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body, "Verification OTP");
        }

        public async Task SendResetPasswordOtpAsync(string toEmail, string otp)
        {
            var subject = "VoiceInfos - Reset Your Password";
            var body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset</title>
                    <style>
                        body {{
                            font-family: 'Helvetica Neue', Arial, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff;
                            border-radius: 10px;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                            overflow: hidden;
                        }}
                        .header {{
                            background-color: #007BFF;
                            color: #ffffff;
                            padding: 20px;
                            text-align: center;
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 26px;
                            font-weight: 500;
                        }}
                        .content {{
                            padding: 30px;
                            color: #333333;
                            line-height: 1.6;
                        }}
                        .otp {{
                            display: inline-block;
                            font-size: 32px;
                            font-weight: bold;
                            color: #007BFF;
                            background-color: #f1f1f1;
                            padding: 15px 25px;
                            border-radius: 8px;
                            margin: 20px 0;
                            letter-spacing: 2px;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            background-color: #007BFF;
                            color: #ffffff;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                            margin-top: 20px;
                        }}
                        .button:hover {{
                            background-color: #0056b3;
                        }}
                        .footer {{
                            background-color: #f4f4f4;
                            padding: 20px;
                            text-align: center;
                            font-size: 14px;
                            color: #666666;
                        }}
                        .footer a {{
                            color: #007BFF;
                            text-decoration: none;
                        }}
                        @media only screen and (max-width: 600px) {{
                            .container {{
                                margin: 10px;
                            }}
                            .content {{
                                padding: 20px;
                            }}
                            .otp {{
                                font-size: 28px;
                                padding: 10px 20px;
                            }}
                            .header h1 {{
                                font-size: 22px;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Reset Your VoiceInfos Password</h1>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>We received a request to reset your password for your VoiceInfos account. Use the One-Time Password (OTP) below to securely reset your password and get back to reading news, posting, and commenting:</p>
                            <div class='otp'>{otp}</div>
                            <p>This OTP is valid for <strong>10 minutes</strong>. For your security, do not share this code with anyone.</p>
                            <p>Once reset, you can continue engaging with the VoiceInfos community. If you didn’t request a password reset, please ignore this email or contact our support team.</p>
                            <a href='mailto:support@voiceinfos.com' class='button'>Contact Support</a>
                        </div>
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} VoiceInfos. All rights reserved.</p>
                            <p><a href='https://www.voiceinfos.com'>Visit VoiceInfos</a> | <a href='mailto:support@voiceinfos.com'>Support</a></p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body, "Password Reset OTP");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string logContext)
        {
            try
            {
                // Create a MimeMessage
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("VoiceInfos", _emailFrom));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = subject;

                // Build the email body
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody // Use HTML for better formatting
                };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                // Send the email using MailKit
                using (var smtpClient = new SmtpClient())
                {
                    await smtpClient.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect); // Use SSL for port 465
                    await smtpClient.AuthenticateAsync(_emailFrom, _emailPassword);
                    await smtpClient.SendAsync(emailMessage);
                    await smtpClient.DisconnectAsync(true);
                }

                // Log email success
                _logger.LogInformation($"{logContext} sent to {toEmail}.");
            }
            catch (Exception ex)
            {
                // Log the error and throw it
                _logger.LogError($"Failed to send {logContext.ToLower()} to {toEmail}: {ex.Message}");
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}