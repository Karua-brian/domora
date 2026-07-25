using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Application.Units.Commands.RegisterUnit;
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


// Application
builder.Services.AddScoped<RegisterOrganizationHandler>();

builder.Services.AddScoped<RegisterPropertyHandler>();

builder.Services.AddScoped<RegisterUnitHandler>();


// Infrastructure
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

builder.Services.AddScoped<IUnitRepository, UnitRepository>();

// Database
builder.Services.AddDbContext<DomoraDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DomoraDb"));
});


// App
var app = builder.Build();

app.MapControllers();

app.Run();
