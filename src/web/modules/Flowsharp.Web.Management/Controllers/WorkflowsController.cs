using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Specifications;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Extensions;
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

        public WorkflowsController(
            IWorkflowStore workflowStore, 
            IWorkflowSerializer workflowSerializer, 
            ITokenFormatterProvider tokenFormatterProvider)
        {
            this.workflowStore = workflowStore;
            this.workflowSerializer = workflowSerializer;
            this.tokenFormatterProvider = tokenFormatterProvider;
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
        public IActionResult Download(string id, [FromQuery]string format, [FromBody]JToken workflowData, CancellationToken cancellationToken)
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