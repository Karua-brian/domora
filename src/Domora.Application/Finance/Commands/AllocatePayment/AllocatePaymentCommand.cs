using Domora.Domain.Common;

namespace Domora.Application.Finance.Commands.AllocatePayment;

public sealed record AllocatePaymentCommand(
    Guid PaymentId,
    Guid InvoiceId,
    Money AllocateAmount
);