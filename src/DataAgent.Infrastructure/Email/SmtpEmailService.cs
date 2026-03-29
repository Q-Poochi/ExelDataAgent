using System.Threading;
using System.Threading.Tasks;
using DataAgent.Application.Interfaces;

namespace DataAgent.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    public Task SendReportEmailAsync(string recipientEmail, string subject, string reportUrl, CancellationToken cancellationToken = default)
    {
        // Integration with n8n or SMTP logic goes here
        return Task.CompletedTask;
    }
}
