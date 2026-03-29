using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;
using DataAgent.API.Hubs;

namespace DataAgent.API.Services;

public class AnalysisNotificationService : IAnalysisNotificationService
{
    private readonly IHubContext<AnalysisHub> _hubContext;

    public AnalysisNotificationService(IHubContext<AnalysisHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyJobUpdate(string jobId, JobUpdateDto dto)
    {
        return _hubContext.Clients.Group(jobId).SendAsync("ReceiveJobUpdate", dto);
    }

    public Task NotifyProgress(string jobId, int percent, string message)
    {
        return _hubContext.Clients.Group(jobId).SendAsync("ReceiveProgress", jobId, percent, message);
    }
}
