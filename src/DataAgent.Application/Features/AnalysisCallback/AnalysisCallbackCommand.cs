using System;
using MediatR;
using DataAgent.Domain.Enums;

namespace DataAgent.Application.Features.AnalysisCallback;

public class AnalysisCallbackCommand : IRequest<bool>
{
    public string RawPayload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class AnalysisCallbackPayload
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? ResultUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
