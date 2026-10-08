using Domora.API.Common.Authorization;
using Domora.API.Finances.Invoices;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers.Finances;

[ApiController]
[Route("api/finances/invoices")]

public sealed class IssueInvoiceController : ControllerBase
{
    private readonly IssueInvoiceHandler _handler;

    public IssueInvoiceController(
        IssueInvoiceHandler handler
    )
    {
        _handler = handler;
    }

    [RequireOrganizationAccess]
    [HttpPost]
    public async Task<IActionResult> Issue(
        IssueInvoiceRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new IssueInvoiceCommand(
            request.LeaseId,
            request.Type,
            new Money(request.Amount, request.Currency),
            request.DueDate
        );

        var response = await _handler.Handle(
            command,
            cancellationToken
        );

        return Created($"api/finances/invoices/{response.Id}", response);
    }
}