using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    [Action(Category = "Rpa.Web", Description = "Opens a new instance of a browser")]
    public class OpenBrowser : Activity
    {
        private readonly IBrowserFactory _factory;
        private readonly RpaWebOptions _options;

        public OpenBrowser(IBrowserFactory factory, IOptions<RpaWebOptions> options)
        {
            _factory = factory;
            _options = options.Value;
        }

        [ActivityInput(Hint = "Open browser in headless mode")]
        public bool UseHeadless { get; set; }
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _factory.OpenAsync(context.CancellationToken);
            return Done();
        }
    }
}