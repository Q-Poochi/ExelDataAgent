using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediatR;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;

namespace DataAgent.Application.Features.Files.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadedFileDto>
{
    private readonly IFileStorageService _fileStorage;
    private readonly IUploadedFileRepository _fileRepository;

    public UploadFileCommandHandler(IFileStorageService fileStorage, IUploadedFileRepository fileRepository)
    {
        _fileStorage = fileStorage;
        _fileRepository = fileRepository;
    }

    public async Task<UploadedFileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var storageKey = await _fileStorage.UploadFileAsync(request.FileStream, request.FileName, request.ContentType, cancellationToken);
        
        var uploadedFile = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = request.FileName,
            StorageKey = storageKey,
            FileSizeBytes = request.FileSize,
            ContentType = request.ContentType,
            UploadedAt = DateTime.UtcNow
        };

        uploadedFile.Id = Guid.NewGuid(); // Ensure Guid is set before saving
        await _fileRepository.AddAsync(uploadedFile, cancellationToken);

        return new UploadedFileDto
        {
            FileId = uploadedFile.Id,
            FileName = uploadedFile.OriginalFileName,
            FileSize = uploadedFile.FileSizeBytes,
            UploadedAt = uploadedFile.UploadedAt
        };
    }
}
