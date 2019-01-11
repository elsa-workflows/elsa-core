using Elsa.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Web.Management.ViewComponents
{
    public class WorkflowEditor : ViewComponent
    {
        public IViewComponentResult Invoke(Workflow workflow)
        {   
            return View(workflow);
        }
    }
}