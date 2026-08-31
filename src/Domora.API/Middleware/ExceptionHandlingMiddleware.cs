using System.Text.Json;
using Domora.Application.Common.Exceptions;
using Domora.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;


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
                ex.Message,
                "Domain validation",
                "https://domora.app/problems/domain-validation"
            );
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource not found while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path
            );

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status404NotFound,
                ex.Message,
                "Not Found conflict",
                "https://domora.app/problems/not-found-conflict"
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
                ex.Message,
                "Resource conflict",
                "https://domora.app/problems/resource-conflict" 
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
                "The resource was modified by another operation.",
                "Concurrency conflict",
                "https://domora.app/problems/concurrency-conflict"
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
                "An unexpected error occurred.",
                "Unhandled conflict",
                "https://domora.app/problems/unhandled-conflict"
            );
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string message,
        string title,
        string type
    )
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = message,
            Type = type,
            Instance = context.Request.Path
            
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}