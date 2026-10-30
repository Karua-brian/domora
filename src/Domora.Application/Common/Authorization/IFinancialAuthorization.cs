namespace Domora.Application.Common.Authorization;

public interface IFinancialAuthorization
{
    Task EnsureCanAsync(
        FinancialCapability capability,
        CancellationToken cancellationToken = default
    );
}
