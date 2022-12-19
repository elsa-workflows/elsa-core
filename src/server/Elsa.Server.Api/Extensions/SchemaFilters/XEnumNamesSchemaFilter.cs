using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Elsa.Server.Api.Extensions.SchemaFilters
{
    public class XEnumNamesSchemaFilter : ISchemaFilter
    {
        private const string NAME = "x-enumNames";

        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            var typeInfo = context.Type;
            if (typeInfo.IsEnum && !model.Extensions.ContainsKey(NAME))
            {
                var names = Enum.GetNames(context.Type);
                var arr = new OpenApiArray();
                arr.AddRange(names.Select(name => new OpenApiString(name)));
                model.Extensions.Add(NAME, arr);
            }
        }
    }
}
