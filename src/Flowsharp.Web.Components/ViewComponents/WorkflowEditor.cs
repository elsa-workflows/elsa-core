using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.Components.ViewComponents
{
    public class WorkflowEditor : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            return View();
        }
    }
}