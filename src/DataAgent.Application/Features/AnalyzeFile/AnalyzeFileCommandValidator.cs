using FluentValidation;

namespace DataAgent.Application.Features.AnalyzeFile;

public class AnalyzeFileCommandValidator : AbstractValidator<AnalyzeFileCommand>
{
    public AnalyzeFileCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage("FileName is required.");
        RuleFor(x => x.FileStream).NotNull().WithMessage("FileStream must not be null.");
        RuleFor(x => x.ContentType).NotEmpty().WithMessage("ContentType is required.");
    }
}
