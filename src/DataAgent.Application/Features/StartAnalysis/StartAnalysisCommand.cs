using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;

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
    private readonly IFileStorageService _fileStorage;
    private readonly IUploadedFileRepository _fileRepository;

    public StartAnalysisCommandHandler(
        IAnalysisJobRepository jobRepository, 
        IJobQueueService jobQueue,
        IFileStorageService fileStorage,
        IUploadedFileRepository fileRepository)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
        _fileStorage = fileStorage;
        _fileRepository = fileRepository;
    }

    public async Task<Guid> Handle(StartAnalysisCommand request, CancellationToken cancellationToken)
    {
        var fileInfo = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (fileInfo == null)
            throw new Exception("File not found");

        string fileUrl = await _fileStorage.GetFileUrlAsync(fileInfo.StorageKey, TimeSpan.FromDays(7));

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
