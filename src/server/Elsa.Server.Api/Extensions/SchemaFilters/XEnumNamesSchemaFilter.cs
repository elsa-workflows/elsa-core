using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Reflection;

namespace Elsa.Server.Api.Extensions.SchemaFilters
{
    public class XEnumNamesSchemaFilter : ISchemaFilter
    {
        private const string nswag = "x-enumNames";
        private const string openapiGenerator = "x-enum-varnames";


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
    }
}
