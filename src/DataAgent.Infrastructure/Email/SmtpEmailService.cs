using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Polly;
using Polly.Retry;
using DataAgent.Application.Interfaces;

namespace DataAgent.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
        
        // Retry 2 times on exception
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Email send failed. Retry {retryCount} after {timeSpan.TotalSeconds} seconds. Error: {exception.Message}");
                });
    }

    public async Task SendReportAsync(string recipientEmail, string? recipientName, string reportUrl, string jobId, CancellationToken cancellationToken = default)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var host = emailSettings["SmtpHost"] ?? Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.mailtrap.io";
        var portStr = emailSettings["SmtpPort"] ?? Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
        var port = int.Parse(portStr);
        var username = emailSettings["SmtpUsername"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var password = emailSettings["SmtpPassword"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var fromEmail = emailSettings["FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "no-reply@dataagent.ai";
        var fromName = emailSettings["FromName"] ?? "DataAgent AI";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(recipientName ?? "Customer", recipientEmail));
        message.Subject = "📊 Báo cáo phân tích dữ liệu của bạn đã sẵn sàng";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; line-height: 1.6; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                <h2 style='color: #2c3e50;'>Chào {recipientName ?? "bạn"},</h2>
                <p>Nền tảng <b>DataAgent AI</b> đã hoàn tất việc phân tích dữ liệu và khởi tạo báo cáo theo yêu cầu của bạn (Job ID: {jobId}).</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{reportUrl}' style='background-color: #3498db; color: white; padding: 14px 28px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>TẢI XUỐNG BÁO CÁO (PDF)</a>
                </div>
                <p style='font-size: 14px; color: #7f8c8d; margin-top: 20px;'>
                    <i>Lưu ý: Báo cáo này có thời hạn lưu trữ. Liên kết sẽ tự động hết hạn sau 30 phút vì lý do bảo mật.</i><br/>
                    Thời gian tạo: {DateTime.Now:dd/MM/yyyy HH:mm}
                </p>
                <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'/>
                <p style='font-size: 12px; color: #95a5a6; text-align: center;'>
                    Đây là email tự động từ hệ thống DataAgent AI. Bạn đang nhận email này vì bạn đã yêu cầu xuất báo cáo.<br/>
                    <a href='#' style='color: #95a5a6; text-decoration: underline;'>Unsubscribe</a>
                </p>
            </body>
            </html>"
        };
        message.Body = bodyBuilder.ToMessageBody();

        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = new SmtpClient();
            
            // Accept all certs for development
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(host, port, SecureSocketOptions.Auto, cancellationToken);
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }
            
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            
            _logger.LogInformation($"Report email successfully sent to {recipientEmail} for Job {jobId}");
        });
    }
}
