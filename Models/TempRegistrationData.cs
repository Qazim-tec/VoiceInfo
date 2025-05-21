namespace VoiceInfo.Models
{
    public class TempRegistrationData
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Store hashed password
        public string OtpCode { get; set; }
        public DateTime OtpExpiration { get; set; }
    }
}