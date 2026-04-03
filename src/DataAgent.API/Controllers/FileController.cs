using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using DataAgent.Application.Features.Files.UploadFile;
using DataAgent.Application.Features.Files.GetFilePreview;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;

namespace DataAgent.API.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUploadedFileRepository _fileRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileParserService _fileParserService;

    public FileController(
        IMediator mediator, 
        IUploadedFileRepository fileRepository, 
        IFileStorageService fileStorageService,
        IFileParserService fileParserService)
    {
        _mediator = mediator;
        _fileRepository = fileRepository;
        _fileStorageService = fileStorageService;
        _fileParserService = fileParserService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("UploadPolicy")]
    public async Task<ActionResult<UploadedFileDto>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        var command = new UploadFileCommand
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            FileStream = file.OpenReadStream()
        };

        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpGet("{fileId}/preview")]
    public async Task<ActionResult<FilePreviewResponse>> GetFilePreview(Guid fileId)
    {
        var query = new GetFilePreviewQuery(fileId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Proxy download endpoint — n8n calls this to download the raw file.
    /// </summary>
    [HttpGet("{fileId}/download")]
    public async Task<ActionResult> DownloadFile(Guid fileId)
    {
        var fileInfo = await _fileRepository.GetByIdAsync(fileId);
        if (fileInfo == null)
            return NotFound($"File with ID {fileId} not found");

        var stream = await _fileStorageService.GetFileStreamAsync(fileInfo.StorageKey);
        return File(stream, fileInfo.ContentType, fileInfo.OriginalFileName);
    }

    /// <summary>
    /// Returns parsed spreadsheet data as JSON — n8n calls this to get structured data
    /// without needing to parse the file itself. Supports CSV and XLSX.
    /// </summary>
    [HttpGet("{fileId}/data")]
    public async Task<ActionResult<FilePreviewResponse>> GetFileData(Guid fileId, [FromQuery] int maxRows = 10000)
    {
        var fileInfo = await _fileRepository.GetByIdAsync(fileId);
        if (fileInfo == null)
            return NotFound($"File with ID {fileId} not found");

        var extension = System.IO.Path.GetExtension(fileInfo.OriginalFileName);
        using var stream = await _fileStorageService.GetFileStreamAsync(fileInfo.StorageKey);
        var parsed = await _fileParserService.ParsePreviewAsync(stream, extension, maxRows);
        
        return Ok(parsed);
    }
}
