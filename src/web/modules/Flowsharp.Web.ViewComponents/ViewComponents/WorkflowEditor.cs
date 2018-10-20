using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Models;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class WorkflowEditor : ViewComponent
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IDisplayManager<IActivity> displayManager;

        public WorkflowEditor(IWorkflowDefinitionStore workflowDefinitionStore, IDisplayManager<IActivity> displayManager)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.displayManager = displayManager;

            workflowDefinitionStore.AddAsync(new WorkflowDefinition
            {
                Workflow = new Workflow(new IActivity[]{ new IfElse()  }, new Connection[0]),
                Id = "1"
            }, CancellationToken.None).Wait();
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            var workflowDefinition = await workflowDefinitionStore.GetAsync(workflowDefinitionId, cancellationToken);
            var activities = workflowDefinition.Workflow.Activities;
            var activityShapes = await BuildActivityShapesAsync(activities, workflowDefinition.Id, "Design");
            var viewModel = new WorkflowEditorViewModel(workflowDefinition, activityShapes);
            
            return View(viewModel);
        }

        private async Task<ICollection<dynamic>> BuildActivityShapesAsync(IEnumerable<IActivity> activities, string workflowDefinitionId, string displayType)
        {
            return await Task.WhenAll(activities.Select((x, i) => BuildActivityShapeAsync(x, i, workflowDefinitionId, displayType)));
        }
        
        private async Task<dynamic> BuildActivityShapeAsync(IActivity activity, int index, string workflowDefinitionId, string displayType)
        {
            dynamic activityShape = await displayManager.BuildDisplayAsync(activity, null, displayType);
            activityShape.Metadata.Type = $"Activity_{displayType}";
            activityShape.Activity = activity;
            activityShape.WorkflowDefinitionId = workflowDefinitionId;
            activityShape.Index = index;
            return activityShape;
        }
    }
}