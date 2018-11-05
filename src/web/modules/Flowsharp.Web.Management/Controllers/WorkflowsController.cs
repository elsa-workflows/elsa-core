using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Specifications;
using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.Management.Controllers
{
    [Route("[controller]")]
    public class WorkflowsController : Controller
    {
        private readonly IWorkflowStore workflowStore;

        public WorkflowsController(IWorkflowStore workflowStore)
        {
            this.workflowStore = workflowStore;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await workflowStore.GetManyAsync(new AllWorkflows(), cancellationToken);
            return View(workflowDefinitions.ToList());
        }


        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflowDefinition = await workflowStore.GetAsync(id, cancellationToken);
            return View(workflowDefinition);
        }
    }
}