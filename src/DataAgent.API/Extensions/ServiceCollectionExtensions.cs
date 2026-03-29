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
        
        services.AddSignalR();
        services.AddScoped<IAnalysisHubContext, SignalRHubContext>();
        
        return services;
    }
}
