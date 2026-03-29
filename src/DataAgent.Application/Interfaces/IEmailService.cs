using System.Threading;
using System.Threading.Tasks;

namespace DataAgent.Application.Interfaces;

public interface IEmailService
{
    Task SendReportEmailAsync(string recipientEmail, string subject, string reportUrl, CancellationToken cancellationToken = default);
}
