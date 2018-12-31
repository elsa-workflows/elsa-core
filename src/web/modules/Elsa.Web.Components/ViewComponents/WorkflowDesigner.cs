using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Web.Components.ViewComponents
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
        
        public async Task<IViewComponentResult> InvokeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
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