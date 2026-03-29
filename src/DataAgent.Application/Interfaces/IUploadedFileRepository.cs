using System;
using System.Threading;
using System.Threading.Tasks;
using DataAgent.Domain.Entities;

namespace DataAgent.Application.Interfaces;

public interface IUploadedFileRepository
{
    Task AddAsync(UploadedFile file, CancellationToken cancellationToken = default);
    Task<UploadedFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
