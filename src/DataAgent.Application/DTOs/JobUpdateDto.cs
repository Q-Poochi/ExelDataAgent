using System;
using DataAgent.Domain.Enums;

namespace DataAgent.Application.DTOs;

public class JobUpdateDto
{
    public string JobId { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public int Progress { get; set; }
    public string? CurrentStep { get; set; }
    public string? Message { get; set; }
    public string? ResultUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
