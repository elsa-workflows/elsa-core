using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Persistence;
using Flowsharp.Web.ViewComponents.Models;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class ActivityLibrary : ViewComponent
    {
        private readonly IDisplayManager<IActivity> displayManager;
        private readonly IActivityInvoker activityInvoker;

        public ActivityLibrary(IDisplayManager<IActivity> displayManager, IActivityInvoker activityInvoker)
        {
            this.displayManager = displayManager;
            this.activityInvoker = activityInvoker;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            var activities = workflow.Activities;
            var activityShapes = await BuildActivityShapesAsync(activities, workflow.Metadata.Id, "Design");
            var viewModel = new WorkflowEditorViewModel(workflow, activityShapes);
            
            return View(viewModel);
        }

        private async Task<ICollection<dynamic>> BuildActivityShapesAsync(IEnumerable<IActivity> activities, string workflowId, string displayType)
        {
            return await Task.WhenAll(activities.Select((x, i) => BuildActivityShapeAsync(x, i, workflowId, displayType)));
        }
        
        private async Task<dynamic> BuildActivityShapeAsync(IActivity activity, int index, string workflowId, string displayType)
        {
            dynamic activityShape = await displayManager.BuildDisplayAsync(activity, null, displayType);
            var customFields = activity.Metadata.CustomFields;
            var designerMetadata = customFields.Get("Designer", () => new ActivityDesignerMetadata());
            
            activityShape.Metadata.Type = $"Activity_{displayType}";
            activityShape.Endpoints = activityInvoker.GetEndpoints(activity).ToList();
            activityShape.Activity = activity;
            activityShape.WorkflowId = workflowId;
            activityShape.Index = index;
            activityShape.Designer = designerMetadata;
            
            return activityShape;
        }
    }
}