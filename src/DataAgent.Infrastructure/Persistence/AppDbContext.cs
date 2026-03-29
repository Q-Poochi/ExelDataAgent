using Microsoft.EntityFrameworkCore;
using DataAgent.Domain.Entities;

namespace DataAgent.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AnalysisJob> AnalysisJobs { get; set; } = null!;
    public DbSet<UploadedFile> UploadedFiles { get; set; } = null!;
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Simple configuration
        modelBuilder.Entity<AnalysisJob>().HasKey(x => x.Id);
        modelBuilder.Entity<UploadedFile>().HasKey(x => x.Id);
        modelBuilder.Entity<EmailLog>().HasKey(x => x.Id);
    }
}
