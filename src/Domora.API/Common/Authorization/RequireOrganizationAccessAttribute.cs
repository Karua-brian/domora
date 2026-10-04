namespace Domora.API.Common.Authorization;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false
)]
public sealed class RequireOrganizationAccessAttribute : Attribute
{
}