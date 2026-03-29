using System.IO;
using MediatR;
using DataAgent.Application.DTOs;

namespace DataAgent.Application.Features.AnalyzeFile;

public class AnalyzeFileCommand : IRequest<UploadFileResponse>
{
    public required Stream FileStream { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public string? Prompt { get; set; }
}
