using FluentValidation;

namespace Freddy.Application.Features.Chat.Commands;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        _ = RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("Conversation ID is required.");

        _ = RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content is required.")
            .MaximumLength(2000)
            .WithMessage("Message content must not exceed 2000 characters.");
    }
}
