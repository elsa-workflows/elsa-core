using System.Collections.Generic;
using AutoMapper;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class ActivityBlueprintConverter : ITypeConverter<IActivityBlueprint, ActivityBlueprintModel?>
    {
        public const string ActivityInputPropertiesKey = "ActivityInputProperties";
        public const string ActivityOutputPropertiesKey = "ActivityOutputProperties";

        public ActivityBlueprintModel Convert(IActivityBlueprint source, ActivityBlueprintModel? destination, ResolutionContext context)
        {
            destination ??= new ActivityBlueprintModel();

            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.DisplayName = source.DisplayName;
            destination.Description = source.Description;
            destination.Type = source.Type;
            destination.ParentId = source.Parent?.Id;
            destination.PersistWorkflow = source.PersistWorkflow;
            destination.LoadWorkflowContext = source.LoadWorkflowContext;
            destination.SaveWorkflowContext = source.SaveWorkflowContext;
            destination.InputProperties = GetInputProperties(source, context);
            destination.OutputProperties = GetOutputProperties(source, context);

            return destination;
        }

        private Variables GetInputProperties(IActivityBlueprint activityBlueprint, ResolutionContext context) => GetProperties(activityBlueprint, context, ActivityInputPropertiesKey);
        private Variables GetOutputProperties(IActivityBlueprint activityBlueprint, ResolutionContext context) => GetProperties(activityBlueprint, context, ActivityOutputPropertiesKey);
        
        private Variables GetProperties(IActivityBlueprint activityBlueprint, ResolutionContext context, string contextKey)
        {
            if (!context.Items.ContainsKey(contextKey))
                return new Variables();

            var dictionary = (IDictionary<string, Variables>) context.Items[contextKey];
            return dictionary[activityBlueprint.Id];
        }
    }
}