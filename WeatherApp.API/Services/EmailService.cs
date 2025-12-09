using System.Net;
using System.Net.Mail;
using System.Text;

namespace WeatherApp.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendWeatherAlertEmailAsync(string toEmail, string city, string country, string alertMessage, string alertType)
        {
            try
            {
                _logger.LogInformation($"[EmailService] Attempting to send email to {toEmail} for {city}");
                
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortStr = _configuration["Email:SmtpPort"] ?? "587";
                var fromEmail = _configuration["Email:FromEmail"];
                var fromPassword = _configuration["Email:FromPassword"];
                var fromName = _configuration["Email:FromName"] ?? "Weather App";

                // Validate configuration
                if (string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("[EmailService] ❌ FromEmail is not configured in appsettings.json");
                    Console.WriteLine("[EmailService] ❌ ERROR: FromEmail is empty. Please configure Email:FromEmail in appsettings.json");
                    return false;
                }

                if (string.IsNullOrEmpty(fromPassword))
                {
                    _logger.LogError("[EmailService] ❌ FromPassword is not configured in appsettings.json");
                    Console.WriteLine("[EmailService] ❌ ERROR: FromPassword is empty. Please configure Email:FromPassword in appsettings.json");
                    return false;
                }

                if (!int.TryParse(smtpPortStr, out var smtpPort))
                {
                    smtpPort = 587;
                    _logger.LogWarning($"[EmailService] Invalid SMTP port, using default: {smtpPort}");
                }

                _logger.LogInformation($"[EmailService] Configuration: Server={smtpServer}, Port={smtpPort}, From={fromEmail}");
                Console.WriteLine($"[EmailService] 📧 Sending email from {fromEmail} to {toEmail}");
                Console.WriteLine($"[EmailService] 📧 SMTP Server: {smtpServer}:{smtpPort}");

                // Create SMTP client with proper configuration
                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 seconds timeout
                };

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = $"Weather Alert: {alertType} - {city}, {country}",
                    Body = BuildEmailBody(city, country, alertMessage, alertType),
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                mailMessage.To.Add(toEmail);

                // Send email
                _logger.LogInformation($"[EmailService] Sending email via SMTP...");
                Console.WriteLine($"[EmailService] 📧 Attempting to send email...");
                
                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation($"[EmailService] ✅ Weather alert email sent successfully to {toEmail} for {city}");
                Console.WriteLine($"[EmailService] ✅ SUCCESS: Email sent to {toEmail} for {city}");
                Console.WriteLine($"[EmailService] ✅ Email subject: Weather Alert: {alertType} - {city}, {country}");
                
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"[EmailService] ❌ SMTP Error sending email to {toEmail}");
                Console.WriteLine($"[EmailService] ❌ SMTP ERROR: {smtpEx.Message}");
                Console.WriteLine($"[EmailService] ❌ Status Code: {smtpEx.StatusCode}");
                
                if (smtpEx.InnerException != null)
                {
                    Console.WriteLine($"[EmailService] ❌ Inner Exception: {smtpEx.InnerException.Message}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EmailService] ❌ Failed to send email to {toEmail}");
                Console.WriteLine($"[EmailService] ❌ ERROR sending email to {toEmail}");
                Console.WriteLine($"[EmailService] ❌ Error Type: {ex.GetType().Name}");
                Console.WriteLine($"[EmailService] ❌ Error Message: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[EmailService] ❌ Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        private string BuildEmailBody(string city, string country, string alertMessage, string alertType)
        {
            var severity = alertType.Contains("Severe") || alertType.Contains("Thunderstorm") ? "High" : "Medium";
            var alertColor = severity == "High" ? "#dc3545" : "#ffc107";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {alertColor}; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .alert-box {{ background-color: white; border-left: 4px solid {alertColor}; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>🌦️ Weather Alert</h2>
        </div>
        <div class=""content"">
            <h3>{city}, {country}</h3>
            <div class=""alert-box"">
                <strong>Alert Type:</strong> {alertType}<br/>
                <strong>Severity:</strong> {severity}<br/>
                <strong>Message:</strong> {alertMessage}
            </div>
            <p>Please take necessary precautions based on this weather alert.</p>
            <div class=""footer"">
                <p>This is an automated alert from Weather App</p>
                <p>You can manage your alert settings in the app.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}

