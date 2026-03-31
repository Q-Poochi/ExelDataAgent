using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;

namespace DataAgent.Application.Features.Emails.SendReportEmail;

public class SendReportEmailCommand : IRequest<bool>
{
    public Guid JobId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
}

public class SendReportEmailCommandHandler : IRequestHandler<SendReportEmailCommand, bool>
{
    private readonly IJobQueueService _jobQueue;
    private readonly IAnalysisJobRepository _jobRepository;

    public SendReportEmailCommandHandler(IJobQueueService jobQueue, IAnalysisJobRepository jobRepository)
    {
        _jobQueue = jobQueue;
        _jobRepository = jobRepository;
    }

    public async Task<bool> Handle(SendReportEmailCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
            throw new Exception("Analysis job not found");

        if (string.IsNullOrEmpty(job.ResultUrl))
            throw new Exception("Analysis is not completed yet, no report available to send.");

        // Enqueue to background job
        _jobQueue.EnqueueEmailJob(request.JobId, request.RecipientEmail, request.RecipientName);
        return true;
    }
}
