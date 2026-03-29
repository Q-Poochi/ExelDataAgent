using System;
using System.Threading.Tasks;
using DataAgent.Application.DTOs;

namespace DataAgent.Application.Interfaces;

public interface IAnalysisHubContext
{
    Task NotifyJobUpdateAsync(Guid jobId, JobStatusDto statusUpdate);
}
