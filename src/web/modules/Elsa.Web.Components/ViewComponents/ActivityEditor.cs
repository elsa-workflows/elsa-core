using Elsa.Web.Components.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Web.Components.ViewComponents
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