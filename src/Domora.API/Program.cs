using Domora.API.Common;
using Domora.API.Middleware;
using Domora.Application.Common.Authentication;
using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Application.Leasing.Commands.EndLease;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Application.Properties.Queries.GetProperty;
using Domora.Application.Units.Commands.RegisterUnit;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
       options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()); 
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
builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();

builder.Services.AddScoped<IOrganizationAccess, OrganizationAccess>();

// Interceptors
builder.Services.AddScoped<OrganizationTransactionInterceptor>();

// Application
builder.Services.AddScoped<RegisterOrganizationHandler>();

builder.Services.AddScoped<RegisterPropertyHandler>();

builder.Services.AddScoped<GetPropertyHandler>();

builder.Services.AddScoped<RegisterUnitHandler>();

builder.Services.AddScoped<RegisterLeaseHandler>();

builder.Services.AddScoped<EndLeaseHandler>();

builder.Services.AddScoped<IssueInvoiceHandler>();

builder.Services.AddScoped<ReceivePaymentHandler>();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the Program class public for testing purposes
public partial class Program { }
