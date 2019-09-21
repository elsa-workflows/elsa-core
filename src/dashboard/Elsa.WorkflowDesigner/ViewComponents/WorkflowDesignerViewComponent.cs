using Elsa.WorkflowDesigner.Models;
using Elsa.WorkflowDesigner.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elsa.WorkflowDesigner.ViewComponents
{
    public class WorkflowDesignerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ActivityDefinition[] activityDefinitions)
        {
            var model = new WorkflowDesignerViewComponentModel
            {
                ActivityDefinitionsJson = GetActivityDefinitionOptions(activityDefinitions)
            };

            return View(model);
        }

        private string GetActivityDefinitionOptions(ActivityDefinition[] activityDefinitions)
        {
            var definitions = activityDefinitions ?? new ActivityDefinition[0];
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.SerializeObject(definitions, settings);
        }
    }
}