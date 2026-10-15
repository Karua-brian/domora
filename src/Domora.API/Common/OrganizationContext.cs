    using Domora.Application.Common.Context;

    namespace Domora.API.Common;

    public sealed class OrganizationContext : IOrganizationContext
    {
        public const string ItemKey =
            "Domora.OrganizationContext.OrganizationId";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ThreadLocal<Guid> _backgroundOrganizationId = new(() => Guid.Empty);

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
                    return _backgroundOrganizationId.Value;;

                if (!httpContext.Items.TryGetValue(
                        ItemKey,
                        out var value))
                {
                    return _backgroundOrganizationId.Value;
                }

                // FALLBACK LOOKUP: If running in a background process, return the thread variable!
                if (httpContext is null)
                    return _backgroundOrganizationId.Value;

                return value is Guid organizationId &&
                    organizationId != Guid.Empty
                    ? organizationId
                    : _backgroundOrganizationId.Value;

                
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
                _backgroundOrganizationId.Value = organizationId;
                return;
            }

            httpContext.Items[ItemKey] = organizationId;
        }
    }