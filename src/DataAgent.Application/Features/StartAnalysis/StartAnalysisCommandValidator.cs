using FluentValidation;

namespace DataAgent.Application.Features.StartAnalysis;

public class StartAnalysisCommandValidator : AbstractValidator<StartAnalysisCommand>
{
    public StartAnalysisCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().WithMessage("FileId is required.");
    }
}
