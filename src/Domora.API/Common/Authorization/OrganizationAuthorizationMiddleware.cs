using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;

namespace Domora.API.Common.Authorization;

public sealed class OrganizationAuthorizationMiddleware
{
    private const string OrganizationHeader = "X-Organization-Id";

    private readonly RequestDelegate _next;

    public OrganizationAuthorizationMiddleware(
        RequestDelegate next
    )
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IUserContext userContext,
        IOrganizationAccess organizationAccess
    )
    {
        var endpoint = httpContext.GetEndpoint();

        var requireOrganizationAccess = 
            endpoint?.Metadata
                .GetMetadata<RequireOrganizationAccessAttribute>()
                is not null;
        
        if (!requireOrganizationAccess)
        {
            await _next(httpContext);
            return;
        }

        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            httpContext.Response.StatusCode = 
                StatusCodes.Status401Unauthorized;

            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(
            OrganizationHeader,
            out var headerValue
        ))
        {
            httpContext.Response.StatusCode = 
                StatusCodes.Status400BadRequest;

            return;
        }

        if (!Guid.TryParse(
            headerValue.ToString(),
            out var organizationId
        ) || organizationId == Guid.Empty
        )
        {
            httpContext.Response.StatusCode = 
                StatusCodes.Status400BadRequest;

            return;
        }

        var canAccess = await organizationAccess.CanAccessAsync(
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

        httpContext.Items[
            OrganizationContext.ItemKey
        ] = organizationId;

        await _next(httpContext);

    }
}