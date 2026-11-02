using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Domora.API.Common.Authorization;

public sealed class DomoraOpenApiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (!endpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });

            operation.Responses ??= [];
            operation.Responses.TryAdd("401", new OpenApiResponse
            {
                Description = "Authentication is required."
            });
            operation.Responses.TryAdd("403", new OpenApiResponse
            {
                Description = "The authenticated user is not authorized."
            });
        }

        if (endpointMetadata.OfType<RequireOrganizationAccessAttribute>().Any())
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Organization-Id",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Organization scope for this request.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "uuid"
                }
            });
        }
    }
}
