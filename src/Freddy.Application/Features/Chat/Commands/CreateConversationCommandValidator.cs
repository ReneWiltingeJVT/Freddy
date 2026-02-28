using FluentValidation;

namespace Freddy.Application.Features.Chat.Commands;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        _ = RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => x.Title is not null)
            .WithMessage("Title must not exceed 200 characters.");
    }
}
