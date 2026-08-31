using Domora.Domain.Common;

namespace Domora.Application.Finance.Commands.ReceivePayment;

public sealed record ReceivePaymentCommand(
    Money Amount,
    string Reference
    );