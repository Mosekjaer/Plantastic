using System.Net.Mail;
using System.Net;
using api.Models;
using api.Configuration;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendPlantHealthEmailAsync(string userEmail, string plantName, PlantHealthAnalysis analysis)
        {
            try
            {
                var username = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? _settings.Username;
                var password = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD") ?? _settings.AppPassword;
                var host = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST") ?? _settings.SmtpHost;
                var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? _settings.SmtpPort.ToString());

                var smtpClient = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(username),
                    Subject = $"Plant Health Alert: {plantName}",
                    IsBodyHtml = true,
                    Body = GenerateEmailBody(plantName, analysis)
                };
                mailMessage.To.Add(userEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Health alert email sent for plant: {PlantName}", plantName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending health alert email for plant: {PlantName}", plantName);
                throw;
            }
        }

        private string GenerateEmailBody(string plantName, PlantHealthAnalysis analysis)
        {
            var issues = string.Join("<br/>", analysis.Issues.Select(i => $"• {i}"));
            var recommendations = string.Join("<br/>", analysis.Recommendations.Select(r => $"• {r}"));

            return $@"
                <h2>Plant Health Alert for {plantName}</h2>
                <p>Current Status: <strong>{analysis.HealthStatus}</strong></p>
                
                <h3>Issues Detected:</h3>
                <p>{issues}</p>
                
                <h3>Recommendations:</h3>
                <p>{recommendations}</p>
                
                <p>This is an automated message from your Plantastic system.</p>";
        }
    }
} 