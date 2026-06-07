using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CoreBanking.BuildingBlocks.Application;

public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse> where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        var name = typeof(TMessage).Name;
        logger.LogInformation("Handling {Message}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(message, ct);
            sw.Stop();
            logger.LogInformation("Handled {Message} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling {Message} after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
