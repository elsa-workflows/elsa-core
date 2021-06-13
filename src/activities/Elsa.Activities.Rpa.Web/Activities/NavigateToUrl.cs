using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    public class WebActivity: Activity
    {
        internal readonly IBrowserFactory _factory;
        internal readonly RpaWebOptions _options;
        [ActivityInput(Hint = "The driver ID assigned when instantiating the browser")]
        public string? DriverId { get; set; }
        public WebActivity(IServiceProvider sp)
        {
            _factory = sp.GetRequiredService<IBrowserFactory>();
            _options = sp.GetRequiredService<IOptions<RpaWebOptions>>().Value;
        }
    }
    [Action(Category = "Rpa.Web", Description = "Navigates to a URL")]
    public class NavigateToUrl : WebActivity
    {
        public NavigateToUrl(IServiceProvider sp) : base(sp)
        {
        }

        [ActivityInput(Hint = "The URL to navigate to")]
        public string? Url { get; set; }
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            _factory.GetDriver(DriverId).Navigate().GoToUrl(Url);
            return Done();
        }
    }
}