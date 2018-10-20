using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.Management.Controllers
{
    [Route("[controller]")]
    public class WorkflowsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
        
        [HttpGet("edit/{id}")]
        public IActionResult Edit(string id)
        {
            return View();
        }
        
        [HttpGet("edit2/edit3")]
        public IActionResult Edit2()
        {
            return View();
        }
    }
}