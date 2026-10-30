    using System.Net;
    using System.Net.Http.Json;
    using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
using Domora.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    namespace Domora.API.Tests.Finances.Payments;

    public sealed class VoidPaymentOrganizationIsolationTests : IClassFixture<DomoraWebApplicationFactory>
    {
        private readonly DomoraWebApplicationFactory _factory;

        public VoidPaymentOrganizationIsolationTests(
            DomoraWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public async Task Organization_cannot_void_another_organization_payment()
        {
            // Arrange
            Guid userAId;
            Guid organizationAId;

            Guid userBId;
            Guid organizationBId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<DomoraDbContext>();

                var userA = await TestData.CreateUserAsync(db);
                var organizationA = await TestData.CreateOrganizationAsync(db);

                await TestData.AddMembershipAsync(
                    db,
                    userA.Id,
                    organizationA.Id
                );

                var userB = await TestData.CreateUserAsync(db);
                var organizationB = await TestData.CreateOrganizationAsync(db);

                await TestData.AddMembershipAsync(
                    db,
                    userB.Id,
                    organizationB.Id
                );

                userAId = userA.Id;
                organizationAId = organizationA.Id;

                userBId = userB.Id;
                organizationBId = organizationB.Id;
            }

            using var clientA = AuthenticatedClientFactory.Create(_factory, userAId);

            clientA.DefaultRequestHeaders.Add(
                "X-Organization-Id",
                organizationAId.ToString()
            );

            var receiveResponse = await clientA.PostAsJsonAsync(
                $"/api/finances/payments",
                new
                {
                    amount = 15000m,
                    currency = "KES",
                    reference = $"ORG-A-REF-{Guid.NewGuid():N}"
                }
            );

            Assert.Equal(
                HttpStatusCode.Created,
                receiveResponse.StatusCode
            );

            var createdPayment = await receiveResponse.Content
                .ReadFromJsonAsync<CreatedPaymentResponse>();

            Assert.NotNull(createdPayment);

            var paymentId = createdPayment.Id;

            // Capture the persisted state before the unauthorized attempt
            string statusBefore;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<DomoraDbContext>();

                var organizationContext = scope.ServiceProvider
                    .GetRequiredService<IOrganizationContext>();

                organizationContext.Set(organizationAId);

                await using var transaction = 
                    await db.Database.BeginTransactionAsync();

                var payment = await db.Payments
                    .AsNoTracking()
                    .SingleAsync(p => p.Id == paymentId);

                statusBefore = payment.Status.ToString();

                await transaction.CommitAsync();
            }

            // Act: Org B attempts to void Org A's payment
            using var clientB =
                AuthenticatedClientFactory.Create(_factory, userBId);

            clientB.DefaultRequestHeaders.Add(
                "X-Organization-Id",
                organizationBId.ToString());

            var voidResponse = await clientB.PostAsync(
                $"/api/finances/payments/{paymentId}/void",
                JsonContent.Create(new { reason = "Cross organization test" })
            );

            // Assert: do not reveal another organization's payment
            Assert.Equal(
                HttpStatusCode.NotFound,
                voidResponse.StatusCode
            );

            // The unauthorized attempt must not mutate the payment
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<DomoraDbContext>();

                var organizationContext = scope.ServiceProvider
                    .GetRequiredService<IOrganizationContext>();

                organizationContext.Set(organizationAId);

                await using var transaction = 
                    await db.Database.BeginTransactionAsync();

                var payment = await db.Payments
                    .AsNoTracking()
                    .SingleAsync(p => p.Id == paymentId);

                Assert.Equal(
                    statusBefore,
                    payment.Status.ToString()
                );

                await transaction.CommitAsync();
            }
        }

        private sealed record CreatedPaymentResponse(Guid Id);
    }
