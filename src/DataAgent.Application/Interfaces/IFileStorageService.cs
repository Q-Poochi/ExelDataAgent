using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataAgent.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(string storageKey, TimeSpan? expiry = null);
    Task<Stream> GetFileStreamAsync(string storageKey, CancellationToken cancellationToken = default);
}
