using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MS.SS.Core.API.OpenApi;

/// <summary>
/// Adds what the framework's problem schemas do not know about: the stable <c>code</c> on every
/// problem and the per-field <c>errorCodes</c> on validation problems.
/// </summary>
public sealed class ProblemDetailsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type != typeof(ProblemDetails) && type != typeof(HttpValidationProblemDetails))
            return Task.CompletedTask;

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Required ??= new HashSet<string>();

        schema.Properties["code"] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "Stable machine-readable code the client localises from. title is a non-contractual fallback."
        };
        schema.Required.Add("code");

        if (type == typeof(HttpValidationProblemDetails))
        {
            schema.Properties["errorCodes"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description =
                    "Field name to the codes of its failures, index-aligned with errors[field]. A code " +
                    "may carry params (limits, bounds) that its localised message interpolates.",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "code" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["code"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            ["params"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String }
                            }
                        }
                    }
                }
            };
        }

        return Task.CompletedTask;
    }
}
