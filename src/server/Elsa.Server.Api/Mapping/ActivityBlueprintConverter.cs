using System.Collections.Generic;
using AutoMapper;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class ActivityBlueprintConverter : ITypeConverter<IActivityBlueprint, ActivityBlueprintModel?>
    {
        public const string ActivityPropertiesKey = "ActivityProperties";
        
        public ActivityBlueprintModel Convert(IActivityBlueprint source, ActivityBlueprintModel? destination, ResolutionContext context)
        {
            destination ??= new ActivityBlueprintModel();
            
            destination.Id = source.Id;
            destination.    Name = source.Name;
            destination.DisplayName = source.DisplayName;
            destination.Description = source.Description;
            destination.Type = source.Type;
            destination.ParentId = source.Parent?.Id;
            destination.PersistWorkflow = source.PersistWorkflow;
            destination.LoadWorkflowContext = source.LoadWorkflowContext;
            destination.SaveWorkflowContext = source.SaveWorkflowContext;
            destination.Properties = GetProperties(source, context);
            
            return destination;
        }

        private Variables GetProperties(IActivityBlueprint activityBlueprint, ResolutionContext context)
        {
            if (!context.Items.ContainsKey(ActivityPropertiesKey)) 
                return new Variables();
            
            var dictionary = (IDictionary<string, Variables>) context.Items[ActivityPropertiesKey];
            return dictionary[activityBlueprint.Id];
        }
    }
}