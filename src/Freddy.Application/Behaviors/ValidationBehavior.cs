using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Freddy.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
#pragma warning disable CA2016 // MediatR RequestHandlerDelegate does not accept CancellationToken
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next().ConfigureAwait(false);
#pragma warning restore CA2016
    }
}
