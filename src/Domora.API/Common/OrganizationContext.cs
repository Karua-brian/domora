using System.Security.Claims;
using Domora.Application.Common.Context;

namespace Domora.API.Common;

public sealed class OrganizationContext : IOrganizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationContext(
        IHttpContextAccessor httpContextAccessor
    )
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OrganizationId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue("organization_id");

            if(!Guid.TryParse(value, out var organizationId))
            {
                throw new InvalidOperationException(
                    "Organization context is unavailable."
                );
            }

            return organizationId;
        }
    }
}