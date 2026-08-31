using Domora.API.Middleware;
using Domora.Application.Common.Exceptions;
using Domora.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Domora.API.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Concurreny_exception_should_return_409_conflict()
    {
        // Arrange
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
           _ => throw new ConcurrencyException(
            "The resource was modified by another operation.",
            null
           ),
           new TestLogger<ExceptionHandlingMiddleware>()
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(
            StatusCodes.Status409Conflict,
            context.Response.StatusCode
        );

    }

    [Fact]
    public async Task Resource_conflict_exception_should_return_409_conflict()
    {
        // Arrange 
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ResourceConflictException(
                "Unit is already occupied.",
                null
            ),
            new TestLogger<ExceptionHandlingMiddleware>()
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(
            StatusCodes.Status409Conflict,
            context.Response.StatusCode
        );

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(
            context.Response.Body
        );

        var body = await reader.ReadToEndAsync();

        Assert.Contains(
            "Unit is already occupied.",
            body
        );

    }

    [Fact]
    public async Task Unexpected_exception_should_return_500_internal_server_error()
    {
        // Arrange
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException(
                "Something went terribly wrong."
            ),
            new TestLogger<ExceptionHandlingMiddleware>()
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            context.Response.StatusCode
        );
    }
}

public sealed class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(
        TState state
    ) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}