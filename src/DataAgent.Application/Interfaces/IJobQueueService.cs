using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataAgent.Application.Interfaces;

public interface IJobQueueService
{
    string EnqueueAnalysisJob(Guid jobId);
    string EnqueueEmailJob(Guid jobId, string email, string? name);
}
