using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;
using DataAgent.Infrastructure.Persistence;

namespace DataAgent.Infrastructure.Queue.Jobs;

public class SendReportEmailJob
{
    private readonly IEmailService _emailService;
    private readonly IAnalysisJobRepository _jobRepository;
    private readonly AppDbContext _dbContext;
    private readonly IAnalysisNotificationService _notificationService;
    private readonly ILogger<SendReportEmailJob> _logger;

    public SendReportEmailJob(
        IEmailService emailService, 
        IAnalysisJobRepository jobRepository,
        AppDbContext dbContext,
        IAnalysisNotificationService notificationService,
        ILogger<SendReportEmailJob> logger)
    {
        _emailService = emailService;
        _jobRepository = jobRepository;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task ExecuteJobAsync(Guid jobId, string email, string? name, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null) throw new Exception($"Job {jobId} not found");
        if (string.IsNullOrEmpty(job.ResultUrl)) throw new Exception($"Job {jobId} has no ResultUrl to email");

        var log = new EmailLog
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            RecipientEmail = email,
            Subject = "📊 Báo cáo phân tích dữ liệu của bạn đã sẵn sàng",
            SentAt = DateTime.UtcNow
        };

        try
        {
            await _emailService.SendReportAsync(email, name, job.ResultUrl, jobId.ToString(), cancellationToken);
            log.IsSuccess = true;
            _dbContext.EmailLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Notify SignalR Client
            await _notificationService.NotifyEmailSent(jobId.ToString(), email);
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            _dbContext.EmailLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, $"Failed to send email to {email} for Job {jobId}");
            throw; // Let Hangfire retry
        }
    }
}
