using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Web.Persistence.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.Management.Controllers
{
    [Route("[controller]")]
    public class WorkflowsController : Controller
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;

        public WorkflowsController(IWorkflowDefinitionStore workflowDefinitionStore)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
        }
        
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await workflowDefinitionStore.ListAsync(cancellationToken);
            return View(workflowDefinitions.ToList());
        }
        
        
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            var workflowDefinition = await workflowDefinitionStore.GetAsync(id, cancellationToken);
            return View(workflowDefinition);
        }
    }
}