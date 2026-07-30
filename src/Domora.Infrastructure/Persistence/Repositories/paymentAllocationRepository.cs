// using Domora.Domain.Finance;

// namespace Domora.Infrastructure.Persistence.Repositories;

// public sealed class PaymentAllocationRepository : IPaymentAllocationRepository
// {
//     private readonly DomoraDbContext _dbContext;

//     public PaymentAllocationRepository(
//         DomoraDbContext dbContext
//     )
//     {
//         _dbContext = dbContext;
//     }
//     public async Task AddAsync(
//         PaymentAllocation paymentAllocation,
//         CancellationToken cancellationToken
//     )
//     {
//         await _dbContext.PaymentAllocations.AddAsync(
//             paymentAllocation,
//             cancellationToken
//         );

//         await _dbContext.SaveChangesAsync(cancellationToken);
//     }

//     public async Task<PaymentAllocation?> GetByIdAsync(
//         Guid id,
//         CancellationToken cancellationToken
//     )
//     {
//         return await _dbContext.PaymentAllocations
//             .FindAsync(
//                 new object[] { id },
//                 cancellationToken
//             );
//     }
// }