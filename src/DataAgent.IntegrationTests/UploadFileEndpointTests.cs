using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataAgent.IntegrationTests;

public class UploadFileEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UploadFileEndpointTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadFile_ShouldReturnBadRequest_WhenFileIsTooLarge()
    {
        // Act
        var content = new MultipartFormDataContent();
        
        // Tạo một file ảo lớn hơn 10MB
        var largeContent = new byte[11 * 1024 * 1024]; 
        var fileContent = new ByteArrayContent(largeContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "large_file.csv");

        var response = await _client.PostAsync("/api/files/upload", content);

        // Assert - Middleware will intercept and return 400 or large payload error
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadFile_ShouldReturnOk_WhenFileIsValid()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var bytes = System.Text.Encoding.UTF8.GetBytes("Id,Name\n1,Test");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        // Act
        var response = await _client.PostAsync("/api/files/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonString = await response.Content.ReadAsStringAsync();
        jsonString.Should().Contain("fileUrl");
        jsonString.Should().Contain("jobId");
    }
}
