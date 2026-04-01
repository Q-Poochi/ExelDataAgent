using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.Minio;
using Xunit;
using DataAgent.Infrastructure.Persistence;

namespace DataAgent.IntegrationTests;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();
    
    // Testcontainers.Minio available in 3.8.0
    private readonly MinioContainer _minioContainer = new MinioBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", _msSqlContainer.GetConnectionString()),
                new System.Collections.Generic.KeyValuePair<string, string?>("MinIO:Endpoint", _minioContainer.GetConnectionString().Replace("http://", "")),
                new System.Collections.Generic.KeyValuePair<string, string?>("MinIO:AccessKey", _minioContainer.GetAccessKey()),
                new System.Collections.Generic.KeyValuePair<string, string?>("MinIO:SecretKey", _minioContainer.GetSecretKey()),
                new System.Collections.Generic.KeyValuePair<string, string?>("MinIO:UseSSL", "false")
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the app's ApplicationDbContext registration.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add ApplicationDbContext using an in-memory database for testing or use the container
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_msSqlContainer.GetConnectionString());
            });

            // Need to build the service provider to apply migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate(); // Apply migrations to the test container
        });
        
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        await _minioContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync().AsTask();
        await _minioContainer.DisposeAsync().AsTask();
    }
}
