using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.Models;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class WorkflowDesigner : ViewComponent
    {
        private readonly IWorkflowStore workflowStore;
        private readonly IActivityLibrary activityLibrary;
        private readonly IActivityDisplayManager displayManager;

        public WorkflowDesigner(
            IWorkflowStore workflowStore, 
            IActivityLibrary activityLibrary,
            IActivityDisplayManager displayManager)
        {
            this.workflowStore = workflowStore;
            this.activityLibrary = activityLibrary;
            this.displayManager = displayManager;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string workflowId, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(workflowId, cancellationToken);
            var activities = workflow.Activities;
            var activityShapes = await BuildActivityShapesAsync(activities, workflow.Metadata.Id, "Design", cancellationToken);
            var viewModel = new WorkflowDesignerViewModel(workflow, activityShapes);
            
            return View(viewModel);
        }

        private async Task<ICollection<dynamic>> BuildActivityShapesAsync(IEnumerable<IActivity> activities, string workflowId, string displayType, CancellationToken cancellationToken)
        {
            return await Task.WhenAll(activities.Select((x, i) => BuildActivityShapeAsync(x, i, workflowId, displayType, cancellationToken)));
        }
        
        private async Task<dynamic> BuildActivityShapeAsync(IActivity activity, int index, string workflowId, string displayType, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activity.Name, cancellationToken);
            dynamic activityShape = await displayManager.BuildDisplayAsync(activity, null, displayType);
            var customFields = activity.Metadata.CustomFields;
            var designerMetadata = customFields.Get("Designer", () => new ActivityDesignerMetadata());
            
            activityShape.Metadata.Type = $"Activity_{displayType}";
            activityShape.Endpoints = activityDescriptor.GetEndpoints().ToList();
            activityShape.Activity = activity;
            activityShape.WorkflowId = workflowId;
            activityShape.Index = index;
            activityShape.Designer = designerMetadata;
            
            return activityShape;
        }
    }
}