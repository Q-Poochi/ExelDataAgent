using System;

namespace DataAgent.Application.DTOs;

public class UploadFileResponse
{
    public Guid JobId { get; set; }
    public string Message { get; set; } = string.Empty;
}
