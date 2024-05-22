namespace Scrabble.Server.Services
{
    /// <summary>
    /// Parameters for Email sending
    /// </summary>
    public class AuthMessageSenderOptions
    {
        public String? EmailEnabled { get; set; }
        public String? SenderEmail { get; set; }
        public String? SenderName { get; set; }
        public String? SmtpUsername { get; set; }
        public String? SmtpAppKey { get; set; }
        public String? SmtpPort { get; set; }
        public String? SmtpHost { get; set; }

    }
}
