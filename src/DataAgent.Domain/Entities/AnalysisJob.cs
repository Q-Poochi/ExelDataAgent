using System;
using DataAgent.Domain.Enums;

namespace DataAgent.Domain.Entities;

public class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Progress { get; set; } = 0;
    public string FileUrl { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public string? ResultUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}
