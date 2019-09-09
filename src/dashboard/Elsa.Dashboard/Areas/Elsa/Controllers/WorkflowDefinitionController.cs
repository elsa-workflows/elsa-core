using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Area("Elsa")]
    [Route("[area]/workflow-definition")]
    public class WorkflowDefinitionController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Content("Workflow Definitions");
        }
    }
}