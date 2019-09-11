using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Elsa.Dashboard.ActionFilters
{
    public class NotifierFilter : IActionFilter
    {
        public const string TempDataKey = "Elsa:Notifications";
        private readonly INotifier notifier;

        public NotifierFilter(INotifier notifier)
        {
            this.notifier = notifier;
        }
        
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var tempData = ((Controller) context.Controller).TempData;

            if (tempData.ContainsKey(TempDataKey))
                return;
            
            tempData.Put(TempDataKey, notifier.Notifications);
        }
    }
}