using Elsa.Serialization;
using Elsa.Services.Models;
using Elsa.WorkflowDesigner.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ActivityDefinition = Elsa.WorkflowDesigner.Models.ActivityDefinition;

namespace Elsa.WorkflowDesigner.ViewComponents
{
    public class WorkflowDesignerViewComponent : ViewComponent
    {
        private readonly IWorkflowSerializer serializer;

        public WorkflowDesignerViewComponent(IWorkflowSerializer serializer)
        {
            this.serializer = serializer;
        }

        public IViewComponentResult Invoke(
            string id,
            ActivityDefinition[]? activityDefinitions = null,
            Workflow? workflow = null)
        {
            var model = new WorkflowDesignerViewComponentModel(
                id,
                Serialize(activityDefinitions ?? new ActivityDefinition[0]),
                Serialize(workflow)
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