using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.Components.ViewComponents
{
    public class WorkflowEditor : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}