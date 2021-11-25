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
        public class CustomTypeNameGenerator : DefaultTypeNameGenerator
        {
            public string Prefix { get; }

            public CustomTypeNameGenerator(string prefix)
            {
                Prefix = prefix;
            }
            
            protected override string Generate(JsonSchema schema, string typeNameHint)
            {
                var result = base.Generate(schema, typeNameHint);
                return result;
            }
        }
        
        public override async ValueTask RenderTypeDeclarationAsync(
            RenderingTypeScriptDefinitions notification,
            ActivityType activityType,
            ActivityDescriptor activityDescriptor,
            ActivityDefinition activityDefinition,
            StringBuilder writer,
            CancellationToken cancellationToken = default)
        {
            var targetTypeSchema = activityDefinition.Properties.First(x => x.Name == nameof(HttpEndpoint.Schema)).Expressions.Values.First();

            if (!string.IsNullOrWhiteSpace(targetTypeSchema))
            {
                var jsonSchema = await JsonSchema.FromJsonAsync(targetTypeSchema, cancellationToken);
                var generator = new TypeScriptGenerator(jsonSchema, new TypeScriptGeneratorSettings
                {
                    TypeStyle = TypeScriptTypeStyle.Interface,
                    TypeScriptVersion = 4,
                    TypeNameGenerator = new CustomTypeNameGenerator(activityDefinition.Name!)
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

            var targetTypeName = activityDefinition.Properties.First(x => x.Name == nameof(HttpEndpoint.TargetType)).Expressions.Values.First();
            var targetTypeSchema = activityDefinition.Properties.First(x => x.Name == nameof(HttpEndpoint.Schema)).Expressions.Values.First();
            var typeScriptType = notification.GetTypeScriptType(propertyType);

            if (!string.IsNullOrWhiteSpace(targetTypeName))
            {
                var type = Type.GetType(targetTypeName);

                if (type != null)
                    typeScriptType = notification.GetTypeScriptType(type);
            }
            else if (!string.IsNullOrWhiteSpace(targetTypeSchema))
            {
                typeScriptType = $"{activityDefinition.Name}Output";
            }

            writer.AppendLine($"{propertyName}(): {typeScriptType}");
        }
    }
}