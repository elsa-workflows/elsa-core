using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Services.Models;

namespace Elsa.Scripting.JavaScript.Providers
{
    public class DefaultActivityTypeDefinitionRenderer : IActivityTypeDefinitionRenderer
    {
        public int Priority => -1;
        public bool GetCanRenderType(ActivityType activityType) => true;

        public virtual async ValueTask RenderTypeDeclarationAsync(
            RenderingTypeScriptDefinitions notification,
            ActivityType activityType,
            ActivityDescriptor activityDescriptor,
            ActivityDefinition activityDefinition,
            StringBuilder writer,
            CancellationToken cancellationToken = default)
        {
            var typeName = activityDefinition.Name;
            var inputProperties = activityDescriptor.InputProperties;
            var outputProperties = activityDescriptor.OutputProperties;
            var interfaceDeclaration = $"declare interface {typeName}";
            writer.AppendLine($"{interfaceDeclaration} {{");

            foreach (var property in inputProperties)
                await RenderActivityPropertyAsync(notification, writer, property.Name, property.Type, activityType, activityDescriptor, activityDefinition, cancellationToken);

            foreach (var property in outputProperties)
                await RenderActivityPropertyAsync(notification, writer, property.Name, property.Type, activityType, activityDescriptor, activityDefinition, cancellationToken);

            writer.AppendLine("}");
        }

        protected virtual ValueTask RenderActivityPropertyAsync(
            RenderingTypeScriptDefinitions notification,
            StringBuilder writer,
            string propertyName,
            Type propertyType,
            ActivityType activityType,
            ActivityDescriptor activityDescriptor,
            ActivityDefinition activityDefinition,
            CancellationToken cancellationToken = default)
        {
            var typeScriptType = notification.GetTypeScriptType(propertyType);
            writer.AppendLine($"{propertyName}(): {typeScriptType}");
            return new ValueTask();
        }
    }
}