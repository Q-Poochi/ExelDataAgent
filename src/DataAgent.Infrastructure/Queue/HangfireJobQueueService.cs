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
}
