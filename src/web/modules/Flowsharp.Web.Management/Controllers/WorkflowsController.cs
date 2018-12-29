using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Specifications;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Extensions;
using Flowsharp.Web.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Web.Management.Controllers
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
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var workflows = await workflowStore.GetManyAsync(new AllWorkflows(), cancellationToken);
            return View(workflows.ToList());
        }

        [HttpGet("new")]
        public IActionResult New(CancellationToken cancellationToken)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(id, cancellationToken);
            return View(workflow);
        }

        [HttpPut("{id}")]
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
    }
}