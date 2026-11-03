using Domora.API.Common.Authorization;
using Domora.Application.Finance.Queries.FinancialVisibility;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers.Finances;

[ApiController]
[Route("api/finances")]
public sealed class FinancialVisibilityController(FinancialVisibilityQueries queries) : ControllerBase
{
    [RequireOrganizationAccess]
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken) =>
        Ok(await queries.GetInvoicesAsync(cancellationToken));

    [RequireOrganizationAccess]
    [HttpGet("tenant-balances")]
    public async Task<IActionResult> GetTenantBalances(CancellationToken cancellationToken) =>
        Ok(await queries.GetTenantBalancesAsync(cancellationToken));

    [RequireOrganizationAccess]
    [HttpGet("arrears")]
    public async Task<IActionResult> GetArrears(CancellationToken cancellationToken) =>
        Ok(await queries.GetArrearsAsync(cancellationToken));
}
