using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class ActivityEditor : ViewComponent
    {        
        public IViewComponentResult Invoke()
        {
            var viewModel = new ActivityEditorViewModel();
            
            return View(viewModel);
        }
    }
}