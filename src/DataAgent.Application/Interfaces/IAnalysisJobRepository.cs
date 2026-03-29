using System;
using System.Threading;
using System.Threading.Tasks;
using DataAgent.Domain.Entities;

namespace DataAgent.Application.Interfaces;

public interface IAnalysisJobRepository
{
    Task<AnalysisJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(AnalysisJob job, CancellationToken cancellationToken = default);
}
