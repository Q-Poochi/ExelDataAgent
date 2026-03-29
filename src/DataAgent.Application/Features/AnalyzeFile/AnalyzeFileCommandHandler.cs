using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;

namespace DataAgent.Application.Features.AnalyzeFile;

public class AnalyzeFileCommandHandler : IRequestHandler<AnalyzeFileCommand, UploadFileResponse>
{
    private readonly IFileStorageService _fileStorage;
    private readonly IAnalysisJobRepository _jobRepository;
    private readonly IJobQueueService _jobQueue;

    public AnalyzeFileCommandHandler(
        IFileStorageService fileStorage,
        IAnalysisJobRepository jobRepository,
        IJobQueueService jobQueue)
    {
        _fileStorage = fileStorage;
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
    }

    public async Task<UploadFileResponse> Handle(AnalyzeFileCommand request, CancellationToken cancellationToken)
    {
        // 1. Upload file to MinIO
        string storageKey = await _fileStorage.UploadFileAsync(request.FileStream, request.FileName, request.ContentType, cancellationToken);
        
        string fileUrl = await _fileStorage.GetFileUrlAsync(storageKey, TimeSpan.FromDays(1));

        // 2. Create Analysis Job Entity
        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            FileUrl = fileUrl,
            Prompt = request.Prompt
        };

        await _jobRepository.AddAsync(job, cancellationToken);

        // 3. Enqueue to Hangfire
        _jobQueue.EnqueueAnalysisJob(job.Id);

        return new UploadFileResponse
        {
            JobId = job.Id,
            Message = "File uploaded and job enqueued successfully."
        };
    }
}
