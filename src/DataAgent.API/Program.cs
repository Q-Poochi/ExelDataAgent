using DataAgent.API.Extensions;
using DataAgent.Application.Extensions;
using DataAgent.Infrastructure.Extensions;
using DataAgent.API.Middlewares;
using Hangfire;
using Serilog;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) => 
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// Add layers
builder.Services.AddPresentation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("UploadPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("AnalysisPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("EmailPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1)
            }));
});

// Configure Advanced Health Checks
var minioEndpoint = builder.Configuration["MinIO:Endpoint"]?.Replace("http://", "").Replace("https://", "");
var minioAccessKey = builder.Configuration["MinIO:AccessKey"];
var minioSecretKey = builder.Configuration["MinIO:SecretKey"];

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy)
    .AddRedis(
        builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379",
        name: "redis",
        failureStatus: HealthStatus.Unhealthy)
    .AddS3(
        s3 => 
        {
            s3.AccessKey = minioAccessKey;
            s3.SecretKey = minioSecretKey;
            s3.OriginalServiceURL = $"http://{minioEndpoint}";
            s3.ForcePathStyle = true;
        },
        name: "minio",
        failureStatus: HealthStatus.Unhealthy);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");

// Add RateLimiter middleware
app.UseRateLimiter();

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHub<DataAgent.API.Hubs.AnalysisHub>("/hubs/analysis");

// Liveness Check
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => false // Execute only base liveness logic
});

// Readiness Check
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(e => new
            {
                Component = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.ToString()
            }),
            TotalDuration = report.TotalDuration
        };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
});

app.Run();
