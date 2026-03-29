using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;

namespace DataAgent.Application.Features.GetJobStatus;

public class GetJobStatusQueryHandler : IRequestHandler<GetJobStatusQuery, JobStatusDto>
{
    private readonly IAnalysisJobRepository _jobRepository;

    public GetJobStatusQueryHandler(IAnalysisJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobStatusDto> Handle(GetJobStatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
        {
            return null!; // Handle not found appropriately in API
        }

        return new JobStatusDto
        {
            JobId = job.Id,
            Status = job.Status,
            Progress = job.Progress,
            ResultUrl = job.ResultUrl,
            ErrorMessage = job.ErrorMessage
        };
    }
}
