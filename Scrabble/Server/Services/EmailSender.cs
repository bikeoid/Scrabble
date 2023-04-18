using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

namespace Scrabble.Server.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;
        private readonly IConfiguration configuration;

        public EmailSender(/*IOptions<AuthMessageSenderOptions> optionsAccessor, */ILogger<EmailSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {

            var emailEnabled = configuration["AuthMessageSenderOptions:EmailEnabled"];
            if (emailEnabled == null || string.Compare(emailEnabled, "true", true) != 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(configuration["AuthMessageSenderOptions:SenderEmail"]))
            {
                throw new Exception("Null AuthMessageSenderOptions:SenderEmail");
            }
            if (string.IsNullOrEmpty(configuration["AuthMessageSenderOptions:SmtpAppKey"]))
            {
                throw new Exception("Null AuthMessageSenderOptions:SenderName");
            }
            if (string.IsNullOrEmpty(configuration["AuthMessageSenderOptions:SmtpUsername"]))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpUsername");
            }
            if (string.IsNullOrEmpty(configuration["AuthMessageSenderOptions:SmtpAppKey"]))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpAppKey");
            }
            if (string.IsNullOrEmpty(configuration["AuthMessageSenderOptions:SmtpPort"]))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpPort");
            }
            if (string.IsNullOrEmpty(configuration["AuthMessageSenderOptions:SmtpHost"]))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpHost");
            }
            await Execute(subject, message, toEmail);
        }

        public async Task Execute(string emailSubject, string emailBody, string toEmailAddress)
        {

            var fromEmailAddress = configuration["AuthMessageSenderOptions:SenderEmail"];
            var fromName = configuration["AuthMessageSenderOptions:SenderName"]; 
            var smtpUsername = configuration["AuthMessageSenderOptions:SmtpUsername"];
            var smtpAppKey = configuration["AuthMessageSenderOptions:SmtpAppKey"];
            var smtpPort = Int32.Parse(configuration["AuthMessageSenderOptions:SmtpPort"]);
            var smtpHost = configuration["AuthMessageSenderOptions:SmtpHost"];

            //Create an email client to send the emails
            var client = new SmtpClient(smtpHost, smtpPort)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpAppKey),
                EnableSsl = true
            };

            MailMessage mailMessage = new MailMessage();
            mailMessage.Subject = emailSubject;
            MailAddress senderEmail = new MailAddress(fromEmailAddress, fromName);
            mailMessage.Sender = senderEmail;
            mailMessage.From = senderEmail;
            mailMessage.To.Add(new MailAddress(toEmailAddress));
            mailMessage.Body = emailBody;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation($"Email to {toEmailAddress}, subject {emailSubject} queued successfully!");


            //// Sendgrid example
            //var client = new SendGridClient(apiKey);
            //var msg = new SendGridMessage()
            //{
            //    From = new EmailAddress("Joe@contoso.com", "Password Recovery"),
            //    Subject = subject,
            //    PlainTextContent = message,
            //    HtmlContent = message
            //};
            //msg.AddTo(new EmailAddress(toEmail));

            //// Disable click tracking.
            //// See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            //msg.SetClickTracking(false, false);
            //var response = await client.SendEmailAsync(msg);
            //_logger.LogInformation(response.IsSuccessStatusCode
            //                       ? $"Email to {toEmail} queued successfully!"
            //                       : $"Failure Email to {toEmail}");
        }
    }
}