using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    public class WebActivity : Activity
    {
        internal readonly IBrowserFactory _factory;
        internal readonly RpaWebOptions _options;
        
        [ActivityInput(Hint = "The driver ID assigned when instantiating the browser. You can use JS functions to retrieve it such as: getVariable() function or activities.{OpenBrowserActivityName}.driverId",
            DefaultSyntax = SyntaxNames.JavaScript, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? DriverId { get; set; }
        protected IActivityExecutionResult? Result { get; set; }
        
        public WebActivity(IServiceProvider sp)
        {
            _factory = sp.GetRequiredService<IBrowserFactory>();
            _options = sp.GetRequiredService<IOptions<RpaWebOptions>>().Value;
        }
        internal string? GetDriverId(ActivityExecutionContext context)
        {
            if (DriverId != default)
                return DriverId;
            
            var activities = context.WorkflowInstance.ActivityData;
                
            var query =
                from activity in activities
                from variable in activity.Value
                where variable.Key == RpaWebConventions.DriverIdKey
                select variable.Value as string;
            DriverId = query.FirstOrDefault();
            return DriverId;
        }
        protected async Task<IActivityExecutionResult> ExecuteDriver(ActivityExecutionContext context, Func<IWebDriver,Task> action)
        {
            var driverId = GetDriverId(context)!;
            
            try
            {
                var driver = _factory.GetDriver(driverId);
                await action(driver);
                
                if (Result != default)
                    return Result;
                
                return Done();
            }
            catch (Exception e)
            {
                if (driverId != default!) 
                    await _factory.CloseBrowserAsync(driverId);
                
                return Fault(e);
            }
        }
    }
}