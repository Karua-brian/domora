using Domora.Application.Common.Context;

namespace Domora.API.Common;

public sealed class OrganizationContext : IOrganizationContext
{
    public const string ItemKey =
        "Domora.OrganizationContext.OrganizationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OrganizationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
                return Guid.Empty;

            if (!httpContext.Items.TryGetValue(
                    ItemKey,
                    out var value))
            {
                return Guid.Empty;
            }

            return value is Guid organizationId &&
                   organizationId != Guid.Empty
                ? organizationId
                : Guid.Empty;
        }
    }

    public void Set(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization ID is required.",
                nameof(organizationId)
            );
        }

        var httpContext =
            _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            throw new InvalidOperationException(
                "Organization context is unavailable."
            );
        }

        httpContext.Items[ItemKey] = organizationId;
    }
}