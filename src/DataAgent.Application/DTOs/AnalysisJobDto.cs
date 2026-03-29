using System;
using DataAgent.Domain.Enums;

namespace DataAgent.Application.DTOs;

public class AnalysisJobDto
{
    public Guid JobId { get; set; }
    public JobStatus Status { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public DateTime CreatedAt { get; set; }
}
