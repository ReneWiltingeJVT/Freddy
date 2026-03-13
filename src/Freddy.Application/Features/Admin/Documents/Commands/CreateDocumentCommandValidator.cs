using FluentValidation;

namespace Freddy.Application.Features.Admin.Documents.Commands;

public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    private static readonly string[] ValidTypes = ["Pdf", "Steps", "Link"];

    public CreateDocumentCommandValidator()
    {
        _ = RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required.");

        _ = RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        _ = RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        _ = RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .Must(type => ValidTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Type must be one of: Pdf, Steps, Link.");

        _ = RuleFor(x => x.FileUrl)
            .NotEmpty().When(x => x.Type is "Pdf" or "Link")
            .WithMessage("FileUrl is required for Pdf and Link document types.");

        _ = RuleFor(x => x.StepsContent)
            .NotEmpty().When(x => x.Type is "Steps")
            .WithMessage("StepsContent is required for Steps document type.");
    }
}
