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
        // If the user is not authenticated, we skip the organization access check.
        if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
        {
            await _next(httpContext);
            return;
        }

        // If the user is authenticated, we check if the X-Organization-Id header is present. 
        // If it's not present, we check if the endpoint requires organization access. 
        // If it does, we return a 400 Bad Request response. 
        // If it doesn't, we skip the organization access check and continue to the next middleware.
        if (!httpContext.Request.Headers.TryGetValue(
                HeaderName,
                out var headerValue))
        {
            var requireOrganization = httpContext
                .GetEndpoint()?
                .Metadata
                .GetMetadata<RequireOrganizationAccessAttribute>() is not null;

            if (requireOrganization)
            {
                httpContext.Response.StatusCode =
                    StatusCodes.Status400BadRequest;

                await httpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        error = "X-Organization-Id header is required."
                    },
                    httpContext.RequestAborted
                );

                return;
            }

            await _next(httpContext);
            return;
        }
        
        // If the header is present, we validate the organization ID and check if the user has access. 
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

        // If the user has access, we set the organization ID in the context for downstream usage.
        organizationContext.Set(organizationId);

        await _next(httpContext);
    }
}