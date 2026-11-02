using Domora.API.Common;
using Domora.API.Common.Authorization;
using Domora.API.Middleware;
using Domora.Application.Common.Authentication;
using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Application.Finance.Commands.IssueMonthlyInvoices;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Application.Finance.Commands.VoidPayment;
using Domora.Application.Leasing.Commands.EndLease;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Application.Organizations.Commands;
using Domora.Application.Organizations.Commands.TransferOwnership;
using Domora.Application.Organizations.Commands.AddOrganizationMember;
using Domora.Application.Organizations.Commands.ChangeOrganizationMemberRole;
using Domora.Application.Organizations.Commands.RemoveOrganizationMember;
using Domora.Application.Organizations.Commands.LeaveOrganization;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Application.Properties.Queries.GetProperty;
using Domora.Application.Units.Commands.RegisterUnit;
using Domora.Application.Users.Commands.RegisterUser;
using Domora.Domain.Finance;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;
using Domora.Domain.Properties;
using Domora.Domain.Units;
using Domora.Infrastructure.Authentication;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using System.Reflection;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // Disable default claim mapping to avoid claim type transformations

// Controllers
builder.Services
    .AddControllers(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        // Apply the authorization policy globally to all controllers
        options.Filters.Add(new AuthorizeFilter(policy));
    })
    .AddJsonOptions(options =>
    {
       options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Domora API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the access token returned by POST /auth/login."
    });
    options.OperationFilter<DomoraOpenApiOperationFilter>();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Http Context
builder.Services.AddHttpContextAccessor();

// Add JWT Access Token Generator
builder.Services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();

// Configure JWT options
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName)
);

// Get JWT options for use in authentication configuration
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Authorization
builder.Services.AddAuthorization();

// Application Context
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();

builder.Services.AddScoped<IOrganizationAccess, OrganizationAccess>();

builder.Services.AddScoped<IFinancialAuthorization, FinancialAuthorization>();

// Interceptors
builder.Services.AddScoped<OrganizationTransactionInterceptor>();

// Application
builder.Services.AddScoped<RegisterUserHandler>();

builder.Services.AddScoped<RegisterOrganizationHandler>();

builder.Services.AddScoped<TransferOwnershipHandler>();

builder.Services.AddScoped<OwnershipMutationGuard>();

builder.Services.AddScoped<AddOrganizationMemberHandler>();

builder.Services.AddScoped<ChangeOrganizationMemberRoleHandler>();

builder.Services.AddScoped<RemoveOrganizationMemberHandler>();

builder.Services.AddScoped<LeaveOrganizationHandler>();

builder.Services.AddScoped<RegisterPropertyHandler>();

builder.Services.AddScoped<GetPropertyHandler>();

builder.Services.AddScoped<RegisterUnitHandler>();

builder.Services.AddScoped<RegisterLeaseHandler>();

builder.Services.AddScoped<EndLeaseHandler>();

builder.Services.AddScoped<IssueInvoiceHandler>();

builder.Services.AddScoped<IssueMonthlyInvoicesHandler>();

builder.Services.AddScoped<ReceivePaymentHandler>();

builder.Services.AddScoped<VoidPaymentHandler>();

builder.Services.AddScoped<AllocatePaymentHandler>();


// Infrastructure
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();

builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

builder.Services.AddScoped<IUnitRepository, UnitRepository>();

builder.Services.AddScoped<ILeaseRepository, LeaseRepository>();

builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<IPaymentAllocationRepository, PaymentAllocationRepository>();

builder.Services.AddScoped<IPaymentVoidRepository, PaymentVoidRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Database
builder.Services.AddDbContext<DomoraDbContext>(
    (serviceProvider, options) =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DomoraDb")
    );

    options.AddInterceptors(
        serviceProvider.GetRequiredService<OrganizationTransactionInterceptor>()
    );

});


// App
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Domora API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();

app.UseMiddleware<OrganizationAuthorizationMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Test endpoints for organization context
if (app.Environment.IsEnvironment("Testing"))
{
    // Endpoint to test organization context without requiring organization access
    app.MapGet(
        "/test/organization-context",
        (IOrganizationContext organizationContext) =>
        Results.Ok(organizationContext.OrganizationId)
    );

    // Endpoint to test organization context with requiring organization access
    app.MapGet(
        "/test/organization-context-required",
        (IOrganizationContext organizationContext) =>
            Results.Ok(organizationContext.OrganizationId)
    )
    .WithMetadata(new RequireOrganizationAccessAttribute());

    app.MapPost(
        "/api/test/trigger-monthly-billing",
        async (
            [FromQuery] string date,
            [FromHeader(Name = "X-Organization-Id")] Guid? organizationId,
            IssueMonthlyInvoicesHandler handler,
            CancellationToken cancellationToken
        ) =>
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
            {
                return Results.BadRequest("Invalid date format. Use YYYY-MM-DD.");
            }

            List<Guid>? targetOrgs = null;
            if (organizationId.HasValue && organizationId.Value != Guid.Empty)
            {
                targetOrgs = new List<Guid> { organizationId.Value };
            }

            var command = new IssueMonthlyInvoicesCommand(parsedDate, targetOrgs);
            var response = await handler.Handle(command, cancellationToken);
            
            return Results.Ok(response);
        }
    );
}

app.Run();

// Make the Program class public for testing purposes
public partial class Program { }
