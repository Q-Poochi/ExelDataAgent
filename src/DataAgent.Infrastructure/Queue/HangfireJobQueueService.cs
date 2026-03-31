using Hangfire;
using DataAgent.Application.Interfaces;
using DataAgent.Infrastructure.Queue.Jobs;

namespace DataAgent.Infrastructure.Queue;

public class HangfireJobQueueService : IJobQueueService
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireJobQueueService(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    public string EnqueueAnalysisJob(Guid jobId)
    {
        return _backgroundJobs.Enqueue<TriggerN8NWorkflowJob>(job => job.ExecuteJobAsync(jobId, CancellationToken.None));
    }

    public string EnqueueEmailJob(Guid jobId, string email, string? name)
    {
        return _backgroundJobs.Enqueue<SendReportEmailJob>(job => job.ExecuteJobAsync(jobId, email, name, CancellationToken.None));
    }
}
