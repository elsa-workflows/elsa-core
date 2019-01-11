using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Web.Components.ViewComponents
{
    public class WorkflowDesigner : ViewComponent
    {
        private readonly IActivityShapeFactory activityShapeFactory;

        public WorkflowDesigner( IActivityShapeFactory activityShapeFactory)
        {
            this.activityShapeFactory = activityShapeFactory;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var activityShapes = await BuildActivityShapesAsync(workflow, cancellationToken);
            var viewModel = new WorkflowDesignerViewModel(workflow, activityShapes);
            
            return View(viewModel);
        }

        private async Task<ICollection<dynamic>> BuildActivityShapesAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            return await Task.WhenAll(workflow.Activities.Select((x, i) => BuildActivityShapeAsync(workflow, x, cancellationToken)));
        }
        
        private async Task<dynamic> BuildActivityShapeAsync(Workflow workflow, IActivity activity, CancellationToken cancellationToken)
        {
            var shape = (dynamic)await activityShapeFactory.BuildDesignShapeAsync(activity, cancellationToken);
            var logEntries = workflow.ExecutionLog.Where(x => x.ActivityId == activity.Id).OrderBy(x => x.Timestamp).ToList();

            shape.LogEntries = logEntries;
            shape.BlockingActivities = workflow.BlockingActivities;
            shape.HasExecuted = logEntries.Any();
            shape.HasFaulted = logEntries.Any(x => x.Faulted);
            shape.IsBlocking = workflow.BlockingActivities.Any(x => x.Id == activity.Id);
            shape.WorkflowIsDefinition = workflow.IsDefinition();

            return shape;
        }
    }
}