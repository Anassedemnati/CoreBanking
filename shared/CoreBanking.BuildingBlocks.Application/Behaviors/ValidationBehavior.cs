using FluentValidation;
using Mediator;
using AppValidationException = CoreBanking.BuildingBlocks.Application.ValidationException;

namespace CoreBanking.BuildingBlocks.Application;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse> where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        if (validators.Any())
        {
            var ctx = new ValidationContext<TMessage>(message);
            var failures = validators
                .Select(v => v.Validate(ctx))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                var errors = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => f.ErrorMessage).ToArray());

                throw new AppValidationException(errors);
            }
        }

        return await next(message, ct);
    }
}
