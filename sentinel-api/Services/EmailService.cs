using MailKit.Net.Smtp;
using MimeKit;

namespace sentinel_api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpServer = _configuration["Smtp:Server"];
            var port = int.Parse(_configuration["Smtp:Port"]);
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Sentinel", "carolinearagao96@gmail.com"));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpServer, port, false);
                await client.AuthenticateAsync(username, password);
                var teste = await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}


