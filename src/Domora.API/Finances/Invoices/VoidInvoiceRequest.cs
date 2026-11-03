namespace Domora.API.Finances.Invoices;

public sealed record VoidInvoiceRequest(string Reason, Guid? ReplacementInvoiceId = null);
