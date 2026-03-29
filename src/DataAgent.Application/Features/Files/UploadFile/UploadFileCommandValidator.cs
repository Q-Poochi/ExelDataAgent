using System;
using System.IO;
using System.Linq;
using FluentValidation;

namespace DataAgent.Application.Features.Files.UploadFile;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(fileName => fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || 
                              fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .csv and .xlsx files are allowed.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(contentType => new[] 
            { 
                "text/csv", 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
            }.Contains(contentType))
            .WithMessage("Invalid MIME type.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(10 * 1024 * 1024)
            .WithMessage("File size must be between 0 and 10MB.");

        RuleFor(x => x.FileStream).NotNull();
    }
}
