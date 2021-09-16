using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Providers;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JsonTypeScriptDefinitionProvider : ITypeScriptDefinitionProvider
    {
        public JsonTypeScriptDefinitionProvider() {}

        public async Task<StringBuilder> GenerateTypeScriptDefinitionsAsync(StringBuilder builder, WorkflowDefinition? workflowDefinition = default, string? context = default, CancellationToken cancellationToken = default)
        {
            if (workflowDefinition == null) 
                return builder;

            foreach (var activity in workflowDefinition.Activities)
            {
                var schemaProperty = activity.Properties.FirstOrDefault(x => x.Name == "Schema");
                if (schemaProperty == null || schemaProperty.Expressions.Count == 0) continue;

                var json = schemaProperty.Expressions.FirstOrDefault().Value;
                if (json == null) continue;

                if (string.IsNullOrWhiteSpace(json)) continue;

                var schema = await JsonSchema.FromJsonAsync(json);
                var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings {
                    TypeStyle = TypeScriptTypeStyle.Class,
                    TypeScriptVersion = 4
                });

                var file = generator.GenerateFile("Json")
                    .Replace("\r\n", "\n")
                    .Replace("export class", "declare class")
                    .Replace("export interface", "declare interface");
                builder.Append(file);
            }

            return builder;
        }
    }
}