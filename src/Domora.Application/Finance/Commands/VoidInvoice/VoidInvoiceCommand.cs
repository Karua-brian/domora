namespace Domora.Application.Finance.Commands.VoidInvoice;

public sealed record VoidInvoiceCommand(Guid InvoiceId, string Reason, Guid? ReplacementInvoiceId = null);
