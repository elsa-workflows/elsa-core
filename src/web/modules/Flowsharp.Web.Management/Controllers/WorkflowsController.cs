using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Specifications;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Extensions;
using Flowsharp.Serialization.Formatters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OrchardCore.Modules;

namespace Flowsharp.Web.Management.Controllers
{
    [Route("[controller]")]
    [IgnoreAntiforgeryToken]
    public class WorkflowsController : Controller
    {
        private readonly IWorkflowStore workflowStore;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly ITokenFormatter tokenFormatter;

        public WorkflowsController(
            IWorkflowStore workflowStore, 
            IWorkflowSerializer workflowSerializer, 
            ITokenFormatter tokenFormatter)
        {
            this.workflowStore = workflowStore;
            this.workflowSerializer = workflowSerializer;
            this.tokenFormatter = tokenFormatter;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var workflows = await workflowStore.GetManyAsync(new AllWorkflows(), cancellationToken);
            return View(workflows.ToList());
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflow = await workflowStore.GetAsync(id, cancellationToken);
            return View(workflow);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody]JToken workflowData, CancellationToken cancellationToken)
        {
            var workflow = await workflowSerializer.DeserializeAsync(workflowData, cancellationToken);
            await workflowStore.UpdateAsync(workflow, cancellationToken);
            return Ok(workflow);
        }
        
        [HttpPost("{id}/download")]
        public IActionResult Download(string id, [FromBody]JToken workflowData, CancellationToken cancellationToken)
        {
            var data = tokenFormatter.ToString(workflowData);
            var bytes = data.ToStream();
            return File(bytes, "application/x-yaml", id);
        }
    }
}