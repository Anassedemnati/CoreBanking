using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreBanking.BuildingBlocks.Infrastructure;

public sealed class ExceptionToProblemDetailsHandler(ILogger<ExceptionToProblemDetailsHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken ct)
    {
        var (status, type, title, extensions) = exception switch
        {
            ValidationException ve => (400, "https://tools.ietf.org/html/rfc7807",
                "Validation failed",
                new Dictionary<string, object?> { ["errors"] = ve.Errors }),
            DomainException de => (422, "https://tools.ietf.org/html/rfc7807",
                de.Message,
                new Dictionary<string, object?> { ["code"] = de.Code }),
            NotFoundException => (404, "https://tools.ietf.org/html/rfc7807",
                exception.Message,
                (Dictionary<string, object?>?)null),
            DbUpdateConcurrencyException => (409, "https://tools.ietf.org/html/rfc7807",
                "A concurrency conflict occurred. Please retry.",
                (Dictionary<string, object?>?)null),
            _ => (500, "https://tools.ietf.org/html/rfc7807",
                "An unexpected error occurred.",
                (Dictionary<string, object?>?)null)
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning(exception, "Handled exception: {Type}", exception.GetType().Name);

        var problem = new ProblemDetails
        {
            Status = status,
            Type = type,
            Title = title,
            Detail = status < 500 ? exception.Message : null
        };

        if (extensions is not null)
            foreach (var (key, value) in extensions)
                problem.Extensions[key] = value;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
