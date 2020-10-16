using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Elsa.Dashboard.ActionFilters
{
    public class NotifierFilter : IActionFilter
    {
        public const string TempDataKey = "Elsa:Notifications";
        private readonly INotifier notifier;
        private readonly ITempDataDictionaryFactory tempDataDictionaryFactory;

        public NotifierFilter(INotifier notifier, ITempDataDictionaryFactory tempDataDictionaryFactory)
        {
            this.notifier = notifier;
            this.tempDataDictionaryFactory = tempDataDictionaryFactory;
        }
        
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var tempData = tempDataDictionaryFactory.GetTempData(context.HttpContext);

            if (tempData.ContainsKey(TempDataKey))
                return;
            
            tempData.Put(TempDataKey, notifier.Notifications);
        }
    }
}