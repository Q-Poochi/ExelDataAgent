using System.IO;
using DataAgent.Application.Features.StartAnalysis;
using FluentAssertions;
using Xunit;

namespace DataAgent.UnitTests;

public class AnalyzeFileCommandValidatorTests
{
    private readonly StartAnalysisCommandValidator _validator;

    public AnalyzeFileCommandValidatorTests()
    {
        _validator = new StartAnalysisCommandValidator();
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenFileUrlIsEmpty()
    {
        // Arrange
        var command = new StartAnalysisCommand { FileUrl = string.Empty, Prompt = "Test" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileUrl");
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenPromptIsEmpty()
    {
        // Arrange
        var command = new StartAnalysisCommand { FileUrl = "http://example.com/file.csv", Prompt = "" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prompt");
    }

    [Fact]
    public void Validator_ShouldNotHaveError_WhenValid()
    {
        // Arrange
        var command = new StartAnalysisCommand 
        { 
            FileUrl = "http://example.com/file.csv", 
            Prompt = "Analyze this data" 
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
