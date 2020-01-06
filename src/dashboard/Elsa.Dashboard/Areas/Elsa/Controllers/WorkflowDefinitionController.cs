using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Dashboard.Areas.Elsa.ViewModels;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Models;
using Elsa.Dashboard.Options;
using Elsa.Dashboard.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Elsa.WorkflowDesigner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Area("Elsa")]
    [Route("[area]/workflow-definition")]
    public class WorkflowDefinitionController : Controller
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowPublisher publisher;
        private readonly IWorkflowSerializer serializer;
        private readonly IOptions<ElsaDashboardOptions> options;
        private readonly INotifier notifier;

        public WorkflowDefinitionController(
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowPublisher publisher,
            IWorkflowSerializer serializer,
            IOptions<ElsaDashboardOptions> options,
            INotifier notifier)
        {
            this.publisher = publisher;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
            this.serializer = serializer;
            this.options = options;
            this.notifier = notifier;
        }

        [HttpGet]
        public async Task<ViewResult> Index(CancellationToken cancellationToken)
        {
            var workflows = await workflowDefinitionStore.ListAsync(
                VersionOptions.LatestOrPublished,
                cancellationToken
            );
            
            var workflowModels = new List<WorkflowDefinitionListItemModel>();

            foreach (var workflow in workflows)
            {
                var workflowModel = await CreateWorkflowDefinitionListItemModelAsync(workflow, cancellationToken);
                workflowModels.Add(workflowModel);
            }
            
            var groups = workflowModels.GroupBy(x => x.WorkflowDefinition.DefinitionId);
            var model = new WorkflowDefinitionListViewModel
            {
                WorkflowDefinitions = groups.ToList()
            };
            return View(model);
        }

        [HttpGet("create")]
        public ViewResult Create()
        {
            var workflowDefinition = publisher.New();

            var model = new WorkflowDefinitionEditModel
            {
                Name = workflowDefinition.Name,
                Json = serializer.Serialize(workflowDefinition, JsonTokenFormatter.FormatName),
                ActivityDefinitions = options.Value.ActivityDefinitions.ToArray(),
                IsSingleton = workflowDefinition.IsSingleton,
                IsDisabled = workflowDefinition.IsDisabled,
                Description = workflowDefinition.Description
            };

            return View(model);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(WorkflowDefinitionEditModel model, CancellationToken cancellationToken)
        {
            var workflow = new WorkflowDefinitionVersion();
            return await SaveAsync(model, workflow, cancellationToken);
        }

        [HttpGet("edit/{id}", Name ="EditWorkflowDefinition")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflowDefinition = await publisher.GetDraftAsync(id, cancellationToken);

            if (workflowDefinition == null)
                return NotFound();

            var workflowModel = new WorkflowModel
            {
                Activities = workflowDefinition.Activities.Select(x => new ActivityModel(x)).ToList(),
                Connections = workflowDefinition.Connections.Select(x => new ConnectionModel(x)).ToList()
            };

            var model = new WorkflowDefinitionEditModel
            {
                Id = workflowDefinition.DefinitionId,
                Name = workflowDefinition.Name,
                Json = serializer.Serialize(workflowModel, JsonTokenFormatter.FormatName),
                Description = workflowDefinition.Description,
                IsSingleton = workflowDefinition.IsSingleton,
                IsDisabled = workflowDefinition.IsDisabled,
                ActivityDefinitions = options.Value.ActivityDefinitions.ToArray(),
                WorkflowModel = workflowModel
            };

            return View(model);
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(
            string id,
            WorkflowDefinitionEditModel model,
            CancellationToken cancellationToken)
        {
            var workflow = await workflowDefinitionStore.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);
            return await SaveAsync(model, workflow, cancellationToken);
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await workflowDefinitionStore.DeleteAsync(id, cancellationToken);
            notifier.Notify("Workflow successfully deleted.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
        }
        
        private async Task<IActionResult> SaveAsync(
            WorkflowDefinitionEditModel model,
            WorkflowDefinitionVersion workflow,
            CancellationToken cancellationToken)
        {
            var postedWorkflow = serializer.Deserialize<WorkflowModel>(model.Json, JsonTokenFormatter.FormatName);

            workflow.Activities = postedWorkflow.Activities
                .Select(x => new ActivityDefinition(x.Id, x.Type, x.State, x.Left, x.Top))
                .ToList();

            workflow.Connections = postedWorkflow.Connections.Select(
                x => new ConnectionDefinition(x.SourceActivityId, x.DestinationActivityId, x.Outcome)).ToList();

            workflow.Description = model.Description;
            workflow.Name = model.Name;
            workflow.IsDisabled = model.IsDisabled;
            workflow.IsSingleton = model.IsSingleton;

            var publish = model.SubmitAction == "publish";

            if (publish)
            {
                workflow = await publisher.PublishAsync(workflow, cancellationToken);
                notifier.Notify("Workflow successfully published.", NotificationType.Success);
            }
            else
            {
                workflow = await publisher.SaveDraftAsync(workflow, cancellationToken);
                notifier.Notify("Workflow successfully saved as a draft.", NotificationType.Success);
            }
            
            return RedirectToRoute("EditWorkflowDefinition", new { id = workflow.DefinitionId });
        }

        private async Task<WorkflowDefinitionListItemModel> CreateWorkflowDefinitionListItemModelAsync(
            WorkflowDefinitionVersion workflowDefinition,
            CancellationToken cancellationToken)
        {
            var instances = await workflowInstanceStore
                .ListByDefinitionAsync(workflowDefinition.DefinitionId, cancellationToken)
                .ToListAsync();

            return new WorkflowDefinitionListItemModel
            {
                WorkflowDefinition = workflowDefinition,
                AbortedCount = instances.Count(x => x.Status == WorkflowStatus.Aborted),
                FaultedCount = instances.Count(x => x.Status == WorkflowStatus.Faulted),
                FinishedCount = instances.Count(x => x.Status == WorkflowStatus.Finished),
                ExecutingCount = instances.Count(x => x.Status == WorkflowStatus.Executing),
            };
        }
    }
}