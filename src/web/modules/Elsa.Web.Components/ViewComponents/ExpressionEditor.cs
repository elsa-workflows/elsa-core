using System.Collections.Generic;
using System.Linq;
using Elsa.Web.Components.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elsa.Web.Components.ViewComponents
{
    public class ExpressionEditor : ViewComponent
    {
        private readonly IEnumerable<IExpressionEvaluator> evaluators;

        public ExpressionEditor(IEnumerable<IExpressionEvaluator> evaluators)
        {
            this.evaluators = evaluators;
        }
        
        public IViewComponentResult Invoke(ExpressionViewModel model, string prefix)
        {
            ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;
            ViewContext.ViewBag.Syntaxes = evaluators.Select(x => new SelectListItem(x.Syntax, x.Syntax));
            return View(model);
        }
    }
}