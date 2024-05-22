namespace Scrabble.Server.Services
{
    public interface IMyEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
