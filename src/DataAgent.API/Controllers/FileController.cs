using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using DataAgent.Application.Features.Files.UploadFile;
using DataAgent.Application.Features.Files.GetFilePreview;
using DataAgent.Application.DTOs;

namespace DataAgent.API.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IMediator _mediator;

    public FileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
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
}
