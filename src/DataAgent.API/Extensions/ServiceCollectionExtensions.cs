using Microsoft.Extensions.DependencyInjection;
using DataAgent.Application.Interfaces;
using DataAgent.API.Services;

namespace DataAgent.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
        });
        // Use the new Notification Service instead of direct HubContext abstraction
        services.AddScoped<IAnalysisNotificationService, AnalysisNotificationService>();
        
        return services;
    }
}
