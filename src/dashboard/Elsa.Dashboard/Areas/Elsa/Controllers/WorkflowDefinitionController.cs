using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Dashboard.Areas.Elsa.ViewModels;
using Elsa.Extensions;
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

        public WorkflowDefinitionController(
            IWorkflowDefinitionStore store,
            IWorkflowPublisher publisher,
            IWorkflowSerializer serializer,
            IIdGenerator idGenerator)
        {
            this.publisher = publisher;
            this.store = store;
            this.serializer = serializer;
            this.idGenerator = idGenerator;
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
            var model = new WorkflowDefinitionViewModel();
            return View(model);
        }
        
        [HttpPost("create")]
        public async Task<IActionResult> Create(WorkflowDefinitionViewModel model, CancellationToken cancellationToken)
        {
            var workflow = !string.IsNullOrWhiteSpace(model.Json) ?
                serializer.Deserialize<WorkflowDefinition>(model.Json, JsonTokenFormatter.FormatName)
                : new WorkflowDefinition();

            workflow.Id = idGenerator.Generate();
            workflow.IsLatest = true;
            workflow.Version = 1;
            
            await store.SaveAsync(workflow, cancellationToken);
            
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
                WorkflowDefinition = workflow,
                Json = serializer.Serialize(workflow, JsonTokenFormatter.FormatName)
            };
            
            return View(model);
        }
    }
}