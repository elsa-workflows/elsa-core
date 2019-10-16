using Elsa.WorkflowDesigner.Models;
using Elsa.WorkflowDesigner.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Workflow = Elsa.Services.Models.Workflow;

namespace Elsa.WorkflowDesigner.ViewComponents
{
    public class WorkflowDesignerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(
            string id,
            DesignerActivityDefinition[]? activityDefinitions = null,
            DesignerWorkflow? workflow = null)
        {
            var model = new WorkflowDesignerViewComponentModel(
                id,
                Serialize(activityDefinitions ?? new DesignerActivityDefinition[0]),
                Serialize(workflow ?? new DesignerWorkflow())
            );

            return View(model);
        }

        private static string? Serialize(object? value)
        {
            if (value == null)
                return null;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(value, settings);
        }
    }
}