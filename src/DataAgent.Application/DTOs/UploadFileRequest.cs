using System.IO;

namespace DataAgent.Application.DTOs;

public class UploadFileRequest
{
    public required Stream Content { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public string? Prompt { get; set; }
}
