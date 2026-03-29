using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAgent.Application.Interfaces;
using DataAgent.Domain.Entities;

namespace DataAgent.Infrastructure.Persistence.Repositories;

public class UploadedFileRepository : IUploadedFileRepository
{
    private readonly AppDbContext _context;

    public UploadedFileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UploadedFile file, CancellationToken cancellationToken = default)
    {
        await _context.UploadedFiles.AddAsync(file, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UploadedFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UploadedFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
