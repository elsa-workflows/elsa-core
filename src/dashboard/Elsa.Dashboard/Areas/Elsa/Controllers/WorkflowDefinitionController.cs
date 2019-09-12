using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Dashboard.Areas.Elsa.ViewModels;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Models;
using Elsa.Dashboard.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Area("Elsa")]
    [Route("[area]/workflow-definition")]
    public class WorkflowDefinitionController : Controller
    {
        private readonly IWorkflowDefinitionStore store;
        private readonly IWorkflowPublisher publisher;
        private readonly IWorkflowSerializer serializer;
        private readonly IIdGenerator idGenerator;
        private readonly INotifier notifier;

        public WorkflowDefinitionController(
            IWorkflowDefinitionStore store,
            IWorkflowPublisher publisher,
            IWorkflowSerializer serializer,
            IIdGenerator idGenerator,
            INotifier notifier)
        {
            this.publisher = publisher;
            this.store = store;
            this.serializer = serializer;
            this.idGenerator = idGenerator;
            this.notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var workflows = await store.ListAsync(VersionOptions.LatestOrPublished, cancellationToken);
            var groups = workflows.GroupBy(x => x.Id);
            var model = new WorkflowDefinitionListViewModel
            {
                WorkflowDefinitions = groups.ToList()
            };
            return View(model);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            var model = new WorkflowDefinitionViewModel
            {
                Name = "New Workflow"
            };
            return View(model);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(WorkflowDefinitionViewModel model, CancellationToken cancellationToken)
        {
            var workflow = !string.IsNullOrWhiteSpace(model.Json)
                ? serializer.Deserialize<WorkflowDefinition>(model.Json, JsonTokenFormatter.FormatName)
                : new WorkflowDefinition();

            workflow.Id = idGenerator.Generate();
            workflow.IsLatest = true;
            workflow.Version = 1;

            await store.SaveAsync(workflow, cancellationToken);

            notifier.Notify("New workflow successfully created.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflow = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

            if (workflow == null)
                return NotFound();
            
            var model = new WorkflowDefinitionViewModel
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                Json = serializer.Serialize(workflow, JsonTokenFormatter.FormatName)
            };

            return View(model);
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(
            string id, 
            WorkflowDefinitionViewModel model,
            CancellationToken cancellationToken)
        {
            var workflow = serializer.Deserialize<WorkflowDefinition>(model.Json, JsonTokenFormatter.FormatName);

            workflow.Id = id;

            var publish = model.SubmitAction == "publish";

            if (publish && !workflow.IsPublished)
            {
                workflow.IsPublished = true;
                workflow.Version++;
            }
            
            await store.SaveAsync(workflow, cancellationToken);

            notifier.Notify("Workflow successfully saved.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await store.DeleteAsync(id, cancellationToken);
            notifier.Notify("Workflow successfully deleted.", NotificationType.Success);
            return RedirectToAction(nameof(Index));
        }
    }
}