using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataAgent.Application.Features.AnalysisCallback;
using DataAgent.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DataAgent.UnitTests;

public class AnalysisCallbackCommandHandlerTests
{
    private readonly Mock<IAnalysisUnitOfWork> _mockUow;
    private readonly Mock<IAnalysisNotificationService> _mockNotificationService;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AnalysisCallbackCommandHandler _handler;

    private readonly string _secret = "TEST_SECRET_KEY_1234567890";

    public AnalysisCallbackCommandHandlerTests()
    {
        _mockUow = new Mock<IAnalysisUnitOfWork>();
        _mockNotificationService = new Mock<IAnalysisNotificationService>();
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(c => c["Analysis:CallbackHmacSecret"]).Returns(_secret);

        _handler = new AnalysisCallbackCommandHandler(
            _mockUow.Object, 
            _mockNotificationService.Object, 
            _mockConfig.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenSignatureIsInvalid()
    {
        // Arrange
        var command = new AnalysisCallbackCommand
        {
            RawPayload = "{\"jobId\":\"some-id\"}",
            Signature = "invalid_signature"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockUow.Verify(u => u.Jobs.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldProcessPayload_WhenSignatureIsValid()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var payload = $"{{\"jobId\":\"{jobId}\", \"status\":2, \"resultUrl\":\"http://example.com/result.pdf\"}}";
        var validSignature = GenerateHmacSignature(payload, _secret);

        var command = new AnalysisCallbackCommand
        {
            RawPayload = payload,
            Signature = validSignature
        };

        var job = new DataAgent.Domain.Entities.AnalysisJob(jobId, "test.csv", "Prompt");
        _mockUow.Setup(u => u.Jobs.GetByIdAsync(jobId)).ReturnsAsync(job);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        job.Status.Should().Be(DataAgent.Domain.Enums.JobStatus.Completed);
        job.ResultUrl.Should().Be("http://example.com/result.pdf");
        _mockUow.Verify(u => u.CommitAsync(), Times.Once);
        _mockNotificationService.Verify(n => n.NotifyJobUpdateAsync(It.IsAny<DataAgent.Application.DTOs.JobStatusDto>()), Times.Once);
    }

    private string GenerateHmacSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
