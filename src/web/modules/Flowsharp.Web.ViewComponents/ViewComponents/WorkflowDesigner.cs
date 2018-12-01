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
        private readonly IActivityShapeFactory activityShapeFactory;

        public WorkflowDesigner(
            IWorkflowStore workflowStore, 
            IActivityShapeFactory activityShapeFactory)
        {
            this.workflowStore = workflowStore;
            this.activityShapeFactory = activityShapeFactory;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string workflowId, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(workflowId, cancellationToken);
            var activities = workflow.Activities;
            var activityShapes = await BuildActivityShapesAsync(activities, cancellationToken);
            var viewModel = new WorkflowDesignerViewModel(workflow, activityShapes);
            
            return View(viewModel);
        }

        private async Task<ICollection<dynamic>> BuildActivityShapesAsync(IEnumerable<IActivity> activities, CancellationToken cancellationToken)
        {
            return await Task.WhenAll(activities.Select((x, i) => BuildActivityShapeAsync(x, cancellationToken)));
        }
        
        private async Task<dynamic> BuildActivityShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            return await activityShapeFactory.BuildDesignShapeAsync(activity, cancellationToken);
        }
    }
}