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
            return View();
        }
        
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpGet("edit/{id}")]
        public IActionResult Edit(string id)
        {
            return View();
        }
    }
}