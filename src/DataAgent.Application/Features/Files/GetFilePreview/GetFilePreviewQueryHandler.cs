using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;

namespace DataAgent.Application.Features.Files.GetFilePreview;

public class GetFilePreviewQueryHandler : IRequestHandler<GetFilePreviewQuery, FilePreviewResponse>
{
    private readonly IUploadedFileRepository _fileRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileParserService _fileParser;

    public GetFilePreviewQueryHandler(IUploadedFileRepository fileRepository, IFileStorageService fileStorage, IFileParserService fileParser)
    {
        _fileRepository = fileRepository;
        _fileStorage = fileStorage;
        _fileParser = fileParser;
    }

    public async Task<FilePreviewResponse> Handle(GetFilePreviewQuery request, CancellationToken cancellationToken)
    {
        var fileInfo = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (fileInfo == null)
            throw new Exception("File not found");

        var extension = Path.GetExtension(fileInfo.OriginalFileName).ToLowerInvariant();
        var fileStream = await _fileStorage.GetFileStreamAsync(fileInfo.StorageKey, cancellationToken);
        
        return await _fileParser.ParsePreviewAsync(fileStream, extension, 50, cancellationToken);
    }
}
