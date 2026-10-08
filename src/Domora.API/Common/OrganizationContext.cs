using System.Security.Claims;
using Domora.Application.Common.Context;

namespace Domora.API.Common;

public sealed class OrganizationContext : IOrganizationContext
{
    public const string ItemKey = "Domora.OrganizationContext.OrganizationId";
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
            var httpContext = _httpContextAccessor.HttpContext;

            if(httpContext is null)
            {
                return Guid.Empty;
            }

            if(!httpContext.Items.TryGetValue(ItemKey, out var value))
            {
                return Guid.Empty;
            }   

            if (value is not Guid organizationId || 
                organizationId == Guid.Empty)
            {
                return Guid.Empty;
            }

            return organizationId;
        }
    }
}