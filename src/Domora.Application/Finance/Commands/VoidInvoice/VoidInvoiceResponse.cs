using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Commands.VoidInvoice;

public sealed record VoidInvoiceResponse(Guid InvoiceId, InvoiceStatus Status, Guid VoidId,
    Guid? ReplacementInvoiceId);
