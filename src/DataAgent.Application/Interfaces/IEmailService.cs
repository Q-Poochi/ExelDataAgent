using System.Threading;
using System.Threading.Tasks;

namespace DataAgent.Application.Interfaces;

public interface IEmailService
{
    Task SendReportAsync(string recipientEmail, string? recipientName, string reportUrl, string jobId, CancellationToken cancellationToken = default);
}
