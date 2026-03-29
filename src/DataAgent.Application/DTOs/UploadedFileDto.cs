using System;

namespace DataAgent.Application.DTOs;

public class UploadedFileDto
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}
