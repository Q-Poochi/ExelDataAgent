using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;

namespace DataAgent.Infrastructure.Persistence.Repositories;

public class AnalysisJobRepository : IAnalysisJobRepository
{
    private readonly AppDbContext _context;

    public AnalysisJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(AnalysisJob job, CancellationToken cancellationToken = default)
    {
        await _context.AnalysisJobs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AnalysisJob job, CancellationToken cancellationToken = default)
    {
        _context.AnalysisJobs.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
