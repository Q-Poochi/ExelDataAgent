using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DataAgent.API.Hubs;

public class AnalysisHub : Hub
{
    private readonly ILogger<AnalysisHub> _logger;

    public AnalysisHub(ILogger<AnalysisHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinJobGroup(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
        _logger.LogInformation("Client {ConnectionId} joined group {JobId}", Context.ConnectionId, jobId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to AnalysisHub: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from AnalysisHub: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
