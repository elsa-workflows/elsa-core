using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Providers;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JsonTypeScriptDefinitionProvider : ITypeScriptDefinitionProvider
    {
        private readonly IEnumerable<ITypeDefinitionProvider> _providers;
        private readonly IMediator _mediator;

        public JsonTypeScriptDefinitionProvider(IEnumerable<ITypeDefinitionProvider> providers, IMediator mediator)
        {
            _providers = providers;
            _mediator = mediator;
        }

        public async Task<StringBuilder> GenerateTypeScriptDefinitionsAsync(StringBuilder builder, WorkflowDefinition? workflowDefinition = default, string? context = default, CancellationToken cancellationToken = default)
        {
            if (workflowDefinition == null) 
                return builder;

            foreach (var activity in workflowDefinition.Activities)
            {
                var schemaProperty = activity.Properties.FirstOrDefault(x => x.Name == "Schema");
                if (schemaProperty == null || schemaProperty.Expressions.Count == 0) continue;

                var json = schemaProperty.Expressions["Json"];
                if (json == null) continue;

                if (string.IsNullOrWhiteSpace(json)) continue;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                string title = data != null ? data.title : "Schema";

                var schema = await JsonSchema.FromJsonAsync(json);
                var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings {
                    TypeStyle = TypeScriptTypeStyle.Class,
                    TypeScriptVersion = 4
                });
                //var code = generator.GenerateTypes();
                var file = generator.GenerateFile(title)
                    .Replace("\r\n", "\n")
                    .Replace("export class", "declare class")
                    .Replace("export interface", "declare interface");
                builder.Append(file);
            }

            return builder;
        }
    }
}