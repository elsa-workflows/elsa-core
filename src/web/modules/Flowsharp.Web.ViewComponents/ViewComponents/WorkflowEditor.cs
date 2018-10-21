using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Persistence;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class WorkflowEditor : ViewComponent
    {
        private readonly IWorkflowStore workflowStore;
        private readonly IDisplayManager<IActivity> displayManager;

        public WorkflowEditor(IWorkflowStore workflowStore, IDisplayManager<IActivity> displayManager)
        {
            this.workflowStore = workflowStore;
            this.displayManager = displayManager;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string workflowId, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(workflowId, cancellationToken);
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
            activityShape.Metadata.Type = $"Activity_{displayType}";
            activityShape.Activity = activity;
            activityShape.WorkflowId = workflowId;
            activityShape.Index = index;
            return activityShape;
        }
    }
}