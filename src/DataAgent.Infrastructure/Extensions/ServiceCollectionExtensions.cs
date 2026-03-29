using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Minio;
using DataAgent.Application.Interfaces;
using DataAgent.Infrastructure.Persistence;
using DataAgent.Infrastructure.Persistence.Repositories;
using DataAgent.Infrastructure.Storage;
using DataAgent.Infrastructure.Queue;
using DataAgent.Infrastructure.Email;
using DataAgent.Infrastructure.Services;
using DataAgent.Infrastructure.Queue.Jobs;

namespace DataAgent.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add Hangfire
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));

        services.AddHangfireServer();

        // Add MinIO
        services.AddMinio(configureClient => configureClient
            .WithEndpoint(configuration["MinIO:Endpoint"])
            .WithCredentials(configuration["MinIO:AccessKey"], configuration["MinIO:SecretKey"])
            .WithSSL(configuration.GetValue<bool>("MinIO:UseSSL"))
            .Build());

        // Register Services
        services.AddHttpClient("N8nClient");
        services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
        services.AddScoped<IUploadedFileRepository, UploadedFileRepository>();
        services.AddScoped<IFileStorageService, MinIOStorageService>();
        services.AddScoped<IFileParserService, CsvExcelParserService>();
        services.AddScoped<IJobQueueService, HangfireJobQueueService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<TriggerN8NWorkflowJob>();

        return services;
    }
}
