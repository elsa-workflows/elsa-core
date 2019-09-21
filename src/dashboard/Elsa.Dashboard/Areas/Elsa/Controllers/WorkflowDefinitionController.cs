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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        private readonly IIdGenerator idGenerator;
        private readonly INotifier notifier;

        public WorkflowDefinitionController(
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowPublisher publisher,
            IWorkflowSerializer serializer,
            IOptions<ElsaDashboardOptions> options,
            IIdGenerator idGenerator,
            INotifier notifier)
        {
            this.publisher = publisher;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
            this.serializer = serializer;
            this.options = options;
            this.idGenerator = idGenerator;
            this.notifier = notifier;
        }

        [HttpGet]
        public async Task<ViewResult> Index(CancellationToken cancellationToken)
        {
            var workflows = await workflowDefinitionStore.ListAsync(
                VersionOptions.LatestOrPublished,
                cancellationToken
            );
            var workflowModelTasks = workflows.Select(
                    async x => await CreateWorkflowDefinitionListItemModelAsync(x, cancellationToken)
                )
                .ToList();
            var workflowModels = workflowModelTasks.Select(x => x.Result);
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
            var workflow = new WorkflowDefinitionVersion
            {
                Name = "New Workflow"
            };

            var model = new WorkflowDefinitionEditModel
            {
                Name = workflow.Name,
                Json = serializer.Serialize(workflow, JsonTokenFormatter.FormatName),
                ActivityDefinitions = options.Value.ActivityDefinitions.ToArray()
            };

            return View(model);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(WorkflowDefinitionEditModel model, CancellationToken cancellationToken)
        {
            var workflow = !string.IsNullOrWhiteSpace(model.Json)
                ? serializer.Deserialize<WorkflowDefinitionVersion>(model.Json, JsonTokenFormatter.FormatName)
                : new WorkflowDefinitionVersion();

            workflow.Id = idGenerator.Generate();
            workflow.DefinitionId = idGenerator.Generate();
            workflow.IsLatest = true;
            workflow.Version = 1;

            await workflowDefinitionStore.SaveAsync(workflow, cancellationToken);

            notifier.Notify("New workflow successfully created.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflow = await workflowDefinitionStore.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (workflow == null)
                return NotFound();

            var model = new WorkflowDefinitionEditModel
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                Json = serializer.Serialize(workflow, JsonTokenFormatter.FormatName),
                ActivityDefinitions = options.Value.ActivityDefinitions.ToArray()
            };

            return View(model);
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(
            string id,
            WorkflowDefinitionEditModel model,
            CancellationToken cancellationToken)
        {
            var workflow = serializer.Deserialize<WorkflowDefinitionVersion>(model.Json, JsonTokenFormatter.FormatName);

            workflow.Id = id;

            var publish = model.SubmitAction == "publish";

            if (publish && !workflow.IsPublished)
            {
                workflow.IsPublished = true;
                workflow.Version++;
            }

            await workflowDefinitionStore.SaveAsync(workflow, cancellationToken);

            notifier.Notify("Workflow successfully saved.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await workflowDefinitionStore.DeleteAsync(id, cancellationToken);
            notifier.Notify("Workflow successfully deleted.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
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