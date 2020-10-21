using System;
using Elsa.Metadata;
using Elsa.WorkflowDesigner.Models;
using Elsa.WorkflowDesigner.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elsa.WorkflowDesigner.ViewComponents
{
    public class WorkflowDesignerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(
            string id,
            ActivityDescriptor[]? activityDefinitions = null,
            WorkflowModel? workflow = null,
            bool? isReadonly = null)
        {
            var model = new WorkflowDesignerViewComponentModel(
                id,
                Serialize(activityDefinitions ?? Array.Empty<ActivityDescriptor>()),
                Serialize(workflow ?? new WorkflowModel()),
                isReadonly.GetValueOrDefault()
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