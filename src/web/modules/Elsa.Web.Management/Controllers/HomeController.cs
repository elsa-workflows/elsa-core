using Microsoft.AspNetCore.Mvc;

namespace Elsa.Web.Management.Controllers
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