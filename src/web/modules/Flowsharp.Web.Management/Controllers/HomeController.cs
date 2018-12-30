using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.Management.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}