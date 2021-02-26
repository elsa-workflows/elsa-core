using System.Threading.Tasks;
using Elsa.Samples.InvokeWorkflowFromController.Workflows;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.InvokeWorkflowFromController.Controllers
{
    [ApiController]
    [Route("launch")]
    public class LaunchController : Controller
    {
        private readonly IWorkflowRunner _workflowRunner;

        public LaunchController(IWorkflowRunner workflowRunner)
        {
            _workflowRunner = workflowRunner;
        }
        
        [HttpGet]
        public async Task<IActionResult> Launch()
        {
            await _workflowRunner.RunWorkflowAsync<RocketWorkflow>();
            
            // Returning empty (the workflow will write an HTTP response).
            return new EmptyResult();
        }
    }
}