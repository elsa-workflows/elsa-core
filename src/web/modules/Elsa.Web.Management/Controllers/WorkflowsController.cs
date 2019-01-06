using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Serialization.Extensions;
using Elsa.Web.Management.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Elsa.Web.Management.Controllers
{
    [Route("[controller]")]
    [IgnoreAntiforgeryToken]
    public class WorkflowsController : Controller
    {
        private readonly IWorkflowStore workflowStore;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly ITokenFormatterProvider tokenFormatterProvider;
        private readonly IIdGenerator idGenerator;

        public WorkflowsController(
            IWorkflowStore workflowStore,
            IWorkflowSerializer workflowSerializer,
            ITokenFormatterProvider tokenFormatterProvider,
            IIdGenerator idGenerator)
        {
            this.workflowStore = workflowStore;
            this.workflowSerializer = workflowSerializer;
            this.tokenFormatterProvider = tokenFormatterProvider;
            this.idGenerator = idGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> Definitions(CancellationToken cancellationToken)
        {
            var workflows = await workflowStore.GetManyAsync(new WorkflowIsDefinition(), cancellationToken).ToListAsync();
            var viewModels = await Task.WhenAll(workflows.Select(async x => await CreateWorkflowSummaryViewModelAsync(x, cancellationToken)).ToList());

            return View(viewModels);
        }

        [HttpGet("{parentId}/finished")]
        public Task<IActionResult> Finished(string parentId, CancellationToken cancellationToken)
        {
            return Instances(parentId, new WorkflowIsFinished(parentId), cancellationToken);
        }
        
        [HttpGet("{parentId}/halted")]
        public Task<IActionResult> Halted(string parentId, CancellationToken cancellationToken)
        {
            return Instances(parentId, new WorkflowIsHalted(parentId), cancellationToken);
        }
        
        [HttpGet("{parentId}/faulted")]
        public Task<IActionResult> Faulted(string parentId, CancellationToken cancellationToken)
        {
            return Instances(parentId, new WorkflowIsFaulted(parentId), cancellationToken);
        }
        
        private async Task<IActionResult> Instances(string parentId, ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken)
        {
            var definition = await workflowStore.GetAsync(parentId, cancellationToken);
            var workflows = await workflowStore.GetManyAsync(specification, cancellationToken).ToListAsync();
            var viewModel = new WorkflowInstancesViewModel
            {
                Definition = definition,
                Instances = workflows
            };

            return View("Instances", viewModel);
        }

        [HttpGet("new")]
        public IActionResult New()
        {
            var workflow = new Workflow
            {
                Metadata = new WorkflowMetadata
                {
                    Name = "New Workflow"
                }
            };
            return View(workflow);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JToken workflowData, CancellationToken cancellationToken)
        {
            var workflow = await workflowSerializer.DeserializeAsync(workflowData, cancellationToken);
            await workflowStore.AddAsync(workflow, cancellationToken);

            return Json(new
            {
                redirect = Url.Action("Edit", new { id = workflow.Metadata.Id })
            });
        }

        [HttpGet("{id}/edit")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(id, cancellationToken);
            return View(workflow);
        }

        [HttpPut("{id}/update")]
        public async Task<IActionResult> Update(string id, [FromBody] JToken workflowData, CancellationToken cancellationToken)
        {
            var workflow = await workflowSerializer.DeserializeAsync(workflowData, cancellationToken);
            await workflowStore.UpdateAsync(workflow, cancellationToken);
            return Ok(workflow);
        }

        [HttpPost("{id}/download")]
        public IActionResult Download(string id, [FromQuery] string format, [FromBody] JToken workflowData, CancellationToken cancellationToken)
        {
            var formatter = tokenFormatterProvider.GetFormatter(format);
            var data = formatter.ToString(workflowData);
            var bytes = data.ToStream();
            var baseName = Path.GetFileNameWithoutExtension(id);
            var fileName = $"{baseName}.{format.ToLowerInvariant()}";
            var contentType = formatter.ContentType;
            return File(bytes, contentType, fileName);
        }
        
        [HttpGet("{id}/details")]
        public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(id, cancellationToken);
            return View(workflow);
        }

        private async Task<WorkflowSummaryViewModel> CreateWorkflowSummaryViewModelAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var instances = await workflowStore.GetManyAsync(new WorkflowIsInstanceOf(workflow.Metadata.Id), cancellationToken).ToListAsync();

            return new WorkflowSummaryViewModel
            {
                Workflow = workflow,
                NumHalted = instances.Count(x => x.IsHalted()),
                NumFaulted = instances.Count(x => x.IsFaulted()),
                NumFinished = instances.Count(x => x.IsFinished())
            };
        }
    }
}