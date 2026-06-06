using Mediator;
using Microsoft.Extensions.Logging;

namespace CoreBanking.BuildingBlocks.Application;

public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse> where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("Handling {Message}", typeof(TMessage).Name);
        var response = await next(message, ct);
        logger.LogInformation("Handled {Message}", typeof(TMessage).Name);
        return response;
    }
}
