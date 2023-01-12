using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Providers;
using Elsa.Services.Models;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;

namespace Elsa.Activities.Http.JavaScript
{
    public class HttpEndpointTypeDefinitionRenderer : DefaultActivityTypeDefinitionRenderer
    {
        public override bool GetCanRenderType(ActivityType activityType) => activityType.Type == typeof(HttpEndpoint);
        
        private static string FirstLetterToUpperCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var a = str.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
        
        public override async ValueTask RenderTypeDeclarationAsync(
            RenderingTypeScriptDefinitions notification,
            ActivityType activityType,
            ActivityDescriptor activityDescriptor,
            ActivityDefinition activityDefinition,
            StringBuilder writer,
            CancellationToken cancellationToken = default)
        {
            var targetTypeSchema = activityDefinition.Properties.FirstOrDefault(x => x.Name == nameof(HttpEndpoint.Schema))?.Expressions.Values.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(targetTypeSchema))
            {
                var jsonSchema = await JsonSchema.FromJsonAsync(targetTypeSchema, cancellationToken);
                var generator = new TypeScriptGenerator(jsonSchema, new TypeScriptGeneratorSettings
                {
                    TypeStyle = TypeScriptTypeStyle.Interface,
                    TypeScriptVersion = 4
                });

                var typeScriptType = $"{activityDefinition.Name}Output";

                var jsonSchemaTypes = generator.GenerateFile(typeScriptType)
                    .Replace("\r\n", "\n")
                    .Replace("export interface", "declare class");

                writer.AppendLine(jsonSchemaTypes);
            }

            await base.RenderTypeDeclarationAsync(notification, activityType, activityDescriptor, activityDefinition, writer, cancellationToken);
        }

        protected override async ValueTask RenderActivityPropertyAsync(
            RenderingTypeScriptDefinitions notification,
            StringBuilder writer,
            string propertyName,
            Type propertyType,
            ActivityType activityType,
            ActivityDescriptor activityDescriptor,
            ActivityDefinition activityDefinition,
            CancellationToken cancellationToken = default)
        {
            if (propertyName != nameof(HttpEndpoint.Output))
            {
                await base.RenderActivityPropertyAsync(notification, writer, propertyName, propertyType, activityType, activityDescriptor, activityDefinition, cancellationToken);
                return;
            }

            var targetTypeName = activityDefinition.Properties.FirstOrDefault(x => x.Name == nameof(HttpEndpoint.TargetType))?.Expressions.Values.FirstOrDefault();
            var targetTypeSchema = activityDefinition.Properties.FirstOrDefault(x => x.Name == nameof(HttpEndpoint.Schema))?.Expressions.Values.FirstOrDefault();
            var typeScriptType = notification.GetTypeScriptType(propertyType);

            if (!string.IsNullOrWhiteSpace(targetTypeName))
            {
                var type = Type.GetType(targetTypeName);

                if (type != null)
                    typeScriptType = notification.GetTypeScriptType(type);
            }
            else if (!string.IsNullOrWhiteSpace(targetTypeSchema))
            {
                typeScriptType = FirstLetterToUpperCase($"{activityDefinition.Name}Output");
            }

            writer.AppendLine($"{propertyName}(): {typeScriptType}");
        }
    }
}
