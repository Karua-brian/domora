using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Domain.Organizations;
using Domora.Domain.Properties;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Controllers
builder.Services.AddControllers();


// Application
builder.Services.AddScoped<RegisterOrganizationHandler>();

builder.Services.AddScoped<RegisterPropertyHandler>();


// Infrastructure
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

// Database
builder.Services.AddDbContext<DomoraDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DomoraDb"));
});


// App
var app = builder.Build();

app.MapControllers();

app.Run();
