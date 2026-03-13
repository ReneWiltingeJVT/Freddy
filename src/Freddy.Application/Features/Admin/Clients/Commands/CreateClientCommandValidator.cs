using FluentValidation;

namespace Freddy.Application.Features.Admin.Clients.Commands;

public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters.");

        RuleFor(x => x.Aliases)
            .NotNull().WithMessage("Aliases must not be null.");
    }
}
