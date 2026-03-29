using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Enums;

namespace DataAgent.Infrastructure.Queue.Jobs;

public class TriggerN8NWorkflowJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAnalysisJobRepository _jobRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<TriggerN8NWorkflowJob> _logger;

    public TriggerN8NWorkflowJob(
        IHttpClientFactory httpClientFactory,
        IAnalysisJobRepository jobRepository,
        IConfiguration config,
        ILogger<TriggerN8NWorkflowJob> logger)
    {
        _httpClientFactory = httpClientFactory;
        _jobRepository = jobRepository;
        _config = config;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ExecuteJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null) throw new Exception($"Job {jobId} not found");

        var webhookUrl = _config["N8N:WebhookUrl"];
        var authToken = _config["N8N:AuthToken"];
        
        // This should probably be the public domain or ngrok in dev. For now we use the configured callback url or construct it.
        // E.g. _config["Api:BaseUrl"] + "/api/analysis/callback"
        var baseUrl = _config["Api:BaseUrl"] ?? "http://localhost:5196";
        var callbackUrl = $"{baseUrl.TrimEnd('/')}/api/analysis/callback";
        
        var payload = new 
        {
            jobId = job.Id,
            fileUrl = job.FileUrl,
            prompt = job.Prompt,
            callbackUrl = callbackUrl
        };

        var client = _httpClientFactory.CreateClient("N8nClient");
        if (!string.IsNullOrEmpty(authToken))
        {
            client.DefaultRequestHeaders.Add("X-N8N-Auth-Token", authToken);
        }

        var response = await client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Trigger N8N workflow failed: {Error}", error);
            
            // Hangfire will catch this and mark as failed, logging the exception and queueing retry
            // Wait, we don't Update Status to Failed here on purpose, let Hangfire retry.
            // But if it's the last attempt, maybe we should? Hangfire OnStateElection handles it if configured.
            // For simplicity, throwing exception is enough.
            throw new Exception($"Failed to trigger n8n workflow. Response: {response.StatusCode} - {error}");
        }

        // Successfully triggered, update to Processing
        job.Status = JobStatus.Processing;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
    }
}
