using System.IO;
using DataAgent.Application.Services;
using FluentAssertions;
using Xunit;

namespace DataAgent.UnitTests;

public class FileTypeDetectionLogicTests
{
    private readonly FileParsingService _service;

    public FileTypeDetectionLogicTests()
    {
        // Setup service with minimal dependencies if needed
        // For simplicity assuming static methods or no constructor parameters
        _service = new FileParsingService();
    }

    [Theory]
    [InlineData("data.csv", "text/csv")]
    [InlineData("report.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void IsSupportedFileType_ShouldReturnTrue_ForValidExtensions(string fileName, string contentType)
    {
        // Act
        var result = _service.IsSupportedFileType(fileName, contentType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("script.js", "application/javascript")]
    public void IsSupportedFileType_ShouldReturnFalse_ForInvalidExtensions(string fileName, string contentType)
    {
        // Act
        var result = _service.IsSupportedFileType(fileName, contentType);

        // Assert
        result.Should().BeFalse();
    }
}
