using Domora.API.Finances.Invoices;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/invoices")]

public sealed class IssueInvoiceController : ControllerBase
{
    private readonly IssueInvoiceHandler _handler;

    public IssueInvoiceController(
        IssueInvoiceHandler handler
    )
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Issue(
        IssueInvoiceRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new IssueInvoiceCommand(
            request.LeaseId,
            new Money(request.Amount, request.Currency),
            request.DueDate
        );

        var response = await _handler.Handle(
            command,
            cancellationToken
        );

        return Created($"api/invoices/{response.Id}", response);
    }
}