using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;

namespace Domora.API.Common.Authorization;

public sealed class OrganizationAuthorizationMiddleware
{
    private const string HeaderName = "X-Organization-Id";

    private readonly RequestDelegate _next;

    public OrganizationAuthorizationMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IOrganizationAccess organizationAccess,
        IOrganizationContext organizationContext,
        IUserContext userContext)
    {
        if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
        {
            await _next(httpContext);
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(
                HeaderName,
                out var headerValue))
        {
            await _next(httpContext);
            return;
        }

        if (!Guid.TryParse(
                headerValue,
                out var organizationId) ||
            organizationId == Guid.Empty)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return;
        }

        var canAccess =
            await organizationAccess.CanAccessAsync(
                userContext.UserId,
                organizationId,
                httpContext.RequestAborted
            );

        if (!canAccess)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return;
        }

        organizationContext.Set(organizationId);

        await _next(httpContext);
    }
}