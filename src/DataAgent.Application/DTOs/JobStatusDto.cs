using System;
using DataAgent.Domain.Enums;

namespace DataAgent.Application.DTOs;

public class JobStatusDto
{
    public Guid JobId { get; set; }
    public JobStatus Status { get; set; }
    public int Progress { get; set; }
    public string? ResultUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
