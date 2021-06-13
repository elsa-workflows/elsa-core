using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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