using Domora.API.Common;
using Domora.API.Middleware;
using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Application.Leasing.Commands.EndLease;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Application.Units.Commands.RegisterUnit;
using Domora.Domain.Finance;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;
using Domora.Domain.Properties;
using Domora.Domain.Units;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();

// Application
builder.Services.AddScoped<RegisterOrganizationHandler>();

builder.Services.AddScoped<RegisterPropertyHandler>();

builder.Services.AddScoped<RegisterUnitHandler>();

builder.Services.AddScoped<RegisterLeaseHandler>();

builder.Services.AddScoped<EndLeaseHandler>();

builder.Services.AddScoped<IssueInvoiceHandler>();

builder.Services.AddScoped<ReceivePaymentHandler>();

builder.Services.AddScoped<AllocatePaymentHandler>();


// Infrastructure
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

builder.Services.AddScoped<IUnitRepository, UnitRepository>();

builder.Services.AddScoped<ILeaseRepository, LeaseRepository>();

builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<IPaymentAllocationRepository, PaymentAllocationRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


// Database
builder.Services.AddDbContext<DomoraDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DomoraDb"));
});


// App
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
