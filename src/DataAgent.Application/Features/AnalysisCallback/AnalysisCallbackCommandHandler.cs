using System;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.Extensions.Configuration;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Enums;

namespace DataAgent.Application.Features.AnalysisCallback;

public class AnalysisCallbackCommandHandler : IRequestHandler<AnalysisCallbackCommand, bool>
{
    private readonly IConfiguration _config;
    private readonly IAnalysisJobRepository _jobRepository;
    private readonly IAnalysisNotificationService _notificationService;
    private readonly Microsoft.Extensions.Logging.ILogger<AnalysisCallbackCommandHandler> _logger;

    public AnalysisCallbackCommandHandler(
        IConfiguration config, 
        IAnalysisJobRepository jobRepository, 
        IAnalysisNotificationService notificationService,
        Microsoft.Extensions.Logging.ILogger<AnalysisCallbackCommandHandler> logger)
    {
        _config = config;
        _jobRepository = jobRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> Handle(AnalysisCallbackCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(">>> Callback received! Payload: {Payload}", request.RawPayload);
        var secret = _config["Analysis:CallbackHmacSecret"];
        if (string.IsNullOrEmpty(secret)) throw new Exception("CallbackHmacSecret is not configured.");

        // Verify HMAC SHA256
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.RawPayload));
        var calculatedSignature = Convert.ToBase64String(hash); // Or Hex, depending on N8N setup. Let's assume Hex for standard webhooks.
        var calculatedSignatureHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        // Support both Base64 and Hex
        if (request.Signature != calculatedSignature && !request.Signature.Equals(calculatedSignatureHex, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid signature.");
        }

        var payload = JsonSerializer.Deserialize<AnalysisCallbackPayload>(request.RawPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload == null) return false;

        var job = await _jobRepository.GetByIdAsync(payload.JobId, cancellationToken);
        if (job == null) return false;

        if (Enum.TryParse<JobStatus>(payload.Status, true, out var parsedStatus))
        {
            job.Status = parsedStatus;
        }
        
        job.Progress = payload.Progress;
        if (!string.IsNullOrEmpty(payload.ResultUrl)) job.ResultUrl = payload.ResultUrl;
        if (!string.IsNullOrEmpty(payload.ErrorMessage)) job.ErrorMessage = payload.ErrorMessage;
        job.UpdatedAt = DateTime.UtcNow;

        await _jobRepository.UpdateAsync(job, cancellationToken);
        _logger.LogInformation("Job {JobId} updated to {Status}", job.Id, job.Status);

        // Notify over SignalR via Group
        var updateDto = new JobUpdateDto
        {
            JobId = job.Id.ToString(),
            Status = job.Status,
            Progress = job.Progress,
            CurrentStep = payload.Status, 
            Message = payload.ErrorMessage ?? "Job update received",
            ResultUrl = job.ResultUrl,
            UpdatedAt = job.UpdatedAt ?? DateTime.UtcNow
        };

        _logger.LogInformation("Pushing SignalR update for job {JobId}...", job.Id);
        await _notificationService.NotifyJobUpdate(job.Id.ToString(), updateDto);

        return true;
    }
}
