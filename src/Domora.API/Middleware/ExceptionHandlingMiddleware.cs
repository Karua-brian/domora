using System.Text.Json;
using Domora.Application.Common.Exceptions;
using Domora.Domain.Common.Exceptions;


namespace Domora.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning(
                ex,
                "Domain validation error while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path
            );

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (ResourceConflictException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource conflict while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path
            );

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                ex.Message 
            );
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrency conflict while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path
            );

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                "The resource was modified by another operation."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path
            );

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred."
            );
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string message
    )
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response)
        );
    }
}