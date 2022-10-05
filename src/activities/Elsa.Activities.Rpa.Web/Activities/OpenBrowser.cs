using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
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

        [ActivityInput(
            Hint = "Open browser in headless mode. Headless means no GUI will be displayed. Often times headless mode is required due to lack of an interactive automation session such as when using Docker or Linux. When running Elsa in an interactive session (e.g. from desktop) you can set this parameter to false and browser GUI will show up",
            DefaultValue = true
            )]
        public bool UseHeadless { get; set; }
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The browser to use",
            Options = new[] { DriverType.Chrome, DriverType.Firefox, DriverType.InternetExplorer, DriverType.Opera },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string BrowserType { get; set; } = DriverType.Chrome;
        [ActivityOutput(Hint = "The driver ID that should be used in other activities to use this window")]
        public string DriverId { get; set; } = DriverType.Chrome;
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var options = new OpenQA.Selenium.Chrome.ChromeOptions();
            if (UseHeadless)
                options.AddArguments("headless");
            var driverId = await _factory.OpenAsync(BrowserType, options, context.CancellationToken);
            Data[RpaWebConventions.DriverIdKey] = driverId;
            DriverId = driverId;
            //this.SaveWorkflowContext = true;
            return Done(driverId);
        }
    }
}