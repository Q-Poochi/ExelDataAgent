using System;
using MediatR;
using DataAgent.Application.DTOs;

namespace DataAgent.Application.Features.GetJobStatus;

public class GetJobStatusQuery : IRequest<JobStatusDto>
{
    public Guid JobId { get; set; }

    public GetJobStatusQuery(Guid jobId)
    {
        JobId = jobId;
    }
}
