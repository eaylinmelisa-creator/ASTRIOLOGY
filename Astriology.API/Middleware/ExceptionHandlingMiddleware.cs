using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Astriology.API.Middleware;

/// <summary>
/// The single funnel for anything that escapes a controller.
/// </summary>
/// <remarks>
/// Expected business failures never arrive here - they travel back as Result values
/// and are mapped by ResultExtensions. What reaches this middleware is genuinely
/// unexpected, so it is logged with a correlation id and the client receives that id
/// and nothing else. Stack traces, SQL and internal paths never leave the server.
/// </remarks>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var correlationId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            if (context.Response.HasStarted)
            {
                // The response is already on the wire; rewriting it would corrupt it.
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen hata",
                Detail = "İstek işlenirken beklenmeyen bir hata oluştu. "
                    + "Sorun sürerse aşağıdaki referans numarasını iletin.",
            };

            problem.Extensions["errorCode"] = "Unexpected";
            problem.Extensions["correlationId"] = correlationId;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
