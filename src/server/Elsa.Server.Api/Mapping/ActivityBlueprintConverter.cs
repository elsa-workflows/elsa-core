using System.Collections.Generic;
using AutoMapper;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class ActivityBlueprintConverter : ITypeConverter<IActivityBlueprint, ActivityBlueprintModel>
    {
        public const string ActivityPropertiesKey = "ActivityProperties";
        
        public ActivityBlueprintModel Convert(IActivityBlueprint source, ActivityBlueprintModel destination, ResolutionContext context)
        {
            return new()
            {
                Id = source.Id,
                Name = source.Name,
                DisplayName = source.DisplayName,
                Description = source.Description,
                Type = source.Type,
                PersistWorkflow = source.PersistWorkflow,
                LoadWorkflowContext = source.LoadWorkflowContext,
                SaveWorkflowContext = source.SaveWorkflowContext,
                Properties = GetProperties(source, context)
            };
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