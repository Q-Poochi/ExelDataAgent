using System.IO;
using MediatR;
using DataAgent.Application.DTOs;

namespace DataAgent.Application.Features.Files.UploadFile;

public class UploadFileCommand : IRequest<UploadedFileDto>
{
    public required Stream FileStream { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
}
