using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using Scrabble.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace Scrabble.Server.Services
{

    public class MyEmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor, ILogger<EmailSender> logger) : IMyEmailSender
    {


        public AuthMessageSenderOptions Options { get; } = optionsAccessor.Value;


        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {


            var emailEnabled = Options.EmailEnabled;
            if (emailEnabled == null || string.Compare(emailEnabled, "true", true) != 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(Options.SenderEmail))
            {
                throw new Exception("Null AuthMessageSenderOptions:SenderEmail");
            }
            if (string.IsNullOrEmpty(Options.SmtpAppKey))
            {
                throw new Exception("Null AuthMessageSenderOptions:SenderName");
            }
            if (string.IsNullOrEmpty(Options.SmtpUsername))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpUsername");
            }
            if (string.IsNullOrEmpty(Options.SmtpAppKey))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpAppKey");
            }
            if (string.IsNullOrEmpty(Options.SmtpPort))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpPort");
            }
            if (string.IsNullOrEmpty(Options.SmtpHost))
            {
                throw new Exception("Null AuthMessageSenderOptions:SmtpHost");
            }
            await Execute(subject, message, toEmail);
        }

        public async Task Execute(string emailSubject, string emailBody, string toEmailAddress)
        {

            var fromEmailAddress = Options.SenderEmail;
            var fromName = Options.SenderName;
            var smtpUsername = Options.SmtpUsername;
            var smtpAppKey = Options.SmtpAppKey;
            var smtpPort = Int32.Parse(Options.SmtpPort);
            var smtpHost = Options.SmtpHost;

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
            mailMessage.IsBodyHtml = false;

            await client.SendMailAsync(mailMessage);
            logger.LogInformation($"Email to {toEmailAddress}, subject {emailSubject} queued successfully!");


        }

    }
}