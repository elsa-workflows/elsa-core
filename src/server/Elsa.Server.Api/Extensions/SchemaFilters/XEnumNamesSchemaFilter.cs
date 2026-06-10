#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using System.Collections.Generic;
#else
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
#endif
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Elsa.Server.Api.Extensions.SchemaFilters
{
    public class XEnumNamesSchemaFilter : ISchemaFilter
    {
        private const string nswag = "x-enumNames";
        private const string openapiGenerator = "x-enum-varnames";


#if NET10_0_OR_GREATER
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum && schema is OpenApiSchema model)
            {
                AddGeneratorSupport(model, context, nswag);
                AddGeneratorSupport(model, context, openapiGenerator);
                AddSwaggerUiSupport(model, context);
            }
        }
        private void AddGeneratorSupport(OpenApiSchema model, SchemaFilterContext context, string generatorType)
        {
            if (model.Extensions?.ContainsKey(generatorType) != true)
            {
                var names = Enum.GetNames(context.Type).Select(x => (JsonNode)x).ToList();
                model.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                model.Extensions.Add(generatorType, new EnumNamesOpenApiExtension(names));
            }
        }

        private void AddSwaggerUiSupport(OpenApiSchema model, SchemaFilterContext context)
        {
            model.Enum?.Clear();
            model.Enum ??= [];
            Enum.GetNames(context.Type)
                .ToList()
                .ForEach(n =>
                {
                    model.Enum.Add((JsonNode)n);
                    model.Type = JsonSchemaType.String;
                    model.Format = null;
                });
        }

        internal class EnumNamesOpenApiExtension : IOpenApiExtension
        {
            public EnumNamesOpenApiExtension(List<JsonNode> enumDescriptions)
            {
                EnumDescriptions = enumDescriptions;
            }

            public List<JsonNode> EnumDescriptions { get; }

            public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
            {
                if (writer is null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }
                writer.WriteStartArray();
                foreach (var description in EnumDescriptions)
                {
                    writer.WriteAny(description);
                }
                writer.WriteEndArray();
            }
        }
#else
        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                AddGeneratorSupport(model, context, nswag);
                AddGeneratorSupport(model, context, openapiGenerator);
                AddSwaggerUiSupport(model, context);
            }

        }

        private void AddGeneratorSupport(OpenApiSchema model, SchemaFilterContext context, string generatorType)
        {
            if (!model.Extensions.ContainsKey(generatorType))
            {
                var names = Enum.GetNames(context.Type);
                var arr = new OpenApiArray();
                arr.AddRange(names.Select(name => new OpenApiString(name)));
                model.Extensions.Add(generatorType, arr);
            }
        }

        private void AddSwaggerUiSupport(OpenApiSchema model, SchemaFilterContext context)
        {
            model.Enum.Clear();
            Enum.GetNames(context.Type)
                .ToList()
                .ForEach(n =>
                {
                    model.Enum.Add(new OpenApiString(n));
                    model.Type = "string";
                    model.Format = null;
                });
        }
#endif

    }
}
