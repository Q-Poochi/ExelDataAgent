using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DataAgent.API.HealthChecks;

/// <summary>
/// Custom MinIO health check that pings the MinIO /minio/health/live endpoint.
/// This avoids issues with AWS SDK package compatibility.
/// </summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly string _endpoint;
    private readonly IHttpClientFactory _httpClientFactory;

    public MinioHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        var raw = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var useSSL = bool.Parse(configuration["MinIO:UseSSL"] ?? "false");
        var scheme = useSSL ? "https" : "http";
        _endpoint = $"{scheme}://{raw.Replace("http://", "").Replace("https://", "")}";
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // MinIO exposes a built-in liveness endpoint at /minio/health/live
            var client = _httpClientFactory.CreateClient("minio-health");
            var response = await client.GetAsync(
                $"{_endpoint}/minio/health/live",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"MinIO is reachable at {_endpoint}")
                : HealthCheckResult.Unhealthy($"MinIO returned {(int)response.StatusCode} at {_endpoint}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"MinIO is unreachable: {ex.Message}");
        }
    }
}
