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

        private readonly string _smtpServer = "smtp-relay.brevo.com"; 
        private readonly int _port = 587; 
        private readonly string _username = "898a10001@smtp-brevo.com";
        private readonly string _password = " ";

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Sentinel", "carolinearagao96@gmail.com"));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _port, false);
                await client.AuthenticateAsync(_username, _password);
                var teste = await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}


