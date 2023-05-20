using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using CrucibleBlogMVC.Models;

namespace CrucibleBlogMVC.Services
{
    public class EmailService : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var emailAddress = _emailSettings.EmailAddress ?? Environment.GetEnvironmentVariable("EmailAddress");
                var emailPassword = _emailSettings.EmailPassword ?? Environment.GetEnvironmentVariable("EmailPassword");
                var emailHost = _emailSettings.EmailHost ?? Environment.GetEnvironmentVariable("EmailHost");
                var emailPort = _emailSettings.EmailPort != 0 ? _emailSettings.EmailPort : int.Parse(Environment.GetEnvironmentVariable("EmailPort")!);

                MimeMessage newEmail = new()
                {
                    Sender = MailboxAddress.Parse(emailAddress)
                };

                newEmail.To.Add(MailboxAddress.Parse(email));
                newEmail.Subject = subject;

                BodyBuilder emailBody = new()
                {
                    HtmlBody = htmlMessage
                };

                newEmail.Body = emailBody.ToMessageBody();

                using SmtpClient smtpClient = new();

                try
                {
                    await smtpClient.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);
                    await smtpClient.AuthenticateAsync(emailAddress, emailPassword);
                    await smtpClient.SendAsync(newEmail);
                    await smtpClient.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    var error = ex.Message;
                    throw;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
