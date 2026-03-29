using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataAgent.Application.Interfaces;

public interface IFileParserService
{
    Task<DTOs.FilePreviewResponse> ParsePreviewAsync(Stream fileStream, string extension, int maxRows = 50, CancellationToken cancellationToken = default);
}
