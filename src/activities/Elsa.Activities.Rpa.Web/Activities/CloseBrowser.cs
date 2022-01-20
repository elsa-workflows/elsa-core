using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    [Action(Category = "Rpa.Web", Description = "Closes a instance of a browser")]
    public class CloseBrowser : WebActivity
    {
        public CloseBrowser(IServiceProvider sp) : base(sp)
        {
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _factory.CloseBrowserAsync(GetDriverId(context));
            return Done();
        }
    }
}