using Domora.API.Common.Authorization;
using Domora.API.Finances.Invoices;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Application.Finance.Commands.VoidInvoice;
using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers.Finances;

[ApiController]
[Route("api/finances/invoices")]

public sealed class IssueInvoiceController : ControllerBase
{
    private readonly IssueInvoiceHandler _handler;
    private readonly VoidInvoiceHandler _voidHandler;

    public IssueInvoiceController(
        IssueInvoiceHandler handler,
        VoidInvoiceHandler voidHandler
    )
    {
        _handler = handler;
        _voidHandler = voidHandler;
    }

    /// <summary>Issues an invoice for a lease in the current organization.</summary>
    [RequireOrganizationAccess]
    [HttpPost]
    [ProducesResponseType(typeof(IssueInvoiceResponse), StatusCodes.Status201Created)]
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

    /// <summary>Voids an unpaid invoice while retaining its financial history.</summary>
    [RequireOrganizationAccess]
    [HttpPost("{invoiceId:guid}/void")]
    [ProducesResponseType(typeof(VoidInvoiceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Void(
        Guid invoiceId, VoidInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await _voidHandler.Handle(new VoidInvoiceCommand(
            invoiceId, request.Reason, request.ReplacementInvoiceId), cancellationToken);
        return Ok(response);
    }
}
