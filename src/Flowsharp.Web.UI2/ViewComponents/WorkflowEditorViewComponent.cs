using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.UI.ViewComponents
{
    public class WorkflowEditorViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            return View();
        }
    }
}