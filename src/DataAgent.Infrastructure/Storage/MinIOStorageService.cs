using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataAgent.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace DataAgent.Infrastructure.Storage;

public class MinIOStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _internalEndpoint;
    private readonly string? _publicEndpoint;

    public MinIOStorageService(IMinioClient minioClient, IConfiguration configuration)
    {
        _minioClient = minioClient;
        _bucketName = configuration["MinIO:BucketName"] ?? "dataagent";
        _internalEndpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        _publicEndpoint = configuration["MinIO:PublicEndpoint"];
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), cancellationToken);
        }

        string storageKey = $"{Guid.NewGuid()}-{fileName}";

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storageKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), cancellationToken);

        return storageKey;
    }

    public async Task<string> GetFileUrlAsync(string storageKey, TimeSpan? expiry = null)
    {
        var expirySpan = expiry ?? TimeSpan.FromDays(1);
        string url = await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storageKey)
            .WithExpiry((int)expirySpan.TotalSeconds));

        // Replace internal endpoint with public endpoint so n8n (inside Docker)
        // can download the file via minio:9000 instead of localhost:9000.
        // The .NET API uses localhost:9000 to connect, but n8n uses the Docker service name.
        if (!string.IsNullOrEmpty(_publicEndpoint) && !string.IsNullOrEmpty(_internalEndpoint))
        {
            url = url.Replace($"http://{_internalEndpoint}", $"http://{_publicEndpoint}");
            url = url.Replace($"https://{_internalEndpoint}", $"http://{_publicEndpoint}");
        }

        return url;
    }

    public async Task<Stream> GetFileStreamAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();
        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storageKey)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream)), cancellationToken);
        
        memoryStream.Position = 0;
        return memoryStream;
    }
}
