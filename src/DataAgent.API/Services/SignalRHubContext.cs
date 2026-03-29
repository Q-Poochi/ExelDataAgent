using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;
using DataAgent.API.Hubs;

namespace DataAgent.API.Services;

public class SignalRHubContext : IAnalysisHubContext
{
    private readonly IHubContext<AnalysisHub> _hubContext;

    public SignalRHubContext(IHubContext<AnalysisHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyJobUpdateAsync(Guid jobId, JobStatusDto statusUpdate)
    {
        return _hubContext.Clients.All.SendAsync("ReceiveJobUpdate", jobId, statusUpdate);
    }
}
