using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;
using DataAgent.Domain.Exceptions;

namespace DataAgent.Application.Features.StartAnalysis;

public class StartAnalysisCommand : IRequest<Guid>
{
    public Guid FileId { get; set; }
    public string? Prompt { get; set; }
}

public class StartAnalysisCommandHandler : IRequestHandler<StartAnalysisCommand, Guid>
{
    private readonly IAnalysisJobRepository _jobRepository;
    private readonly IJobQueueService _jobQueue;
    private readonly IUploadedFileRepository _fileRepository;
    private readonly IConfiguration _config;

    public StartAnalysisCommandHandler(
        IAnalysisJobRepository jobRepository, 
        IJobQueueService jobQueue,
        IUploadedFileRepository fileRepository,
        IConfiguration config)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
        _fileRepository = fileRepository;
        _config = config;
    }

    public async Task<Guid> Handle(StartAnalysisCommand request, CancellationToken cancellationToken)
    {
        var fileInfo = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (fileInfo == null)
            throw new NotFoundException($"File with ID {request.FileId} not found");

        // Use the proxy endpoints instead of presigned MinIO URL.
        // n8n runs inside Docker and cannot access MinIO presigned URLs signed with localhost.
        var publicBaseUrl = _config["Api:PublicBaseUrl"] ?? _config["Api:BaseUrl"] ?? "http://host.docker.internal:5196";
        string fileUrl = $"{publicBaseUrl.TrimEnd('/')}/api/files/{request.FileId}/data?maxRows=10000";

        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            FileUrl = fileUrl,
            Prompt = request.Prompt
        };

        await _jobRepository.AddAsync(job, cancellationToken);

        _jobQueue.EnqueueAnalysisJob(job.Id);

        return job.Id;
    }
}

