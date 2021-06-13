using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rpa.Web
{
    public class WebActivity : Activity
    {
        internal readonly IBrowserFactory _factory;
        internal readonly RpaWebOptions _options;
        [ActivityInput(Hint = "The driver ID assigned when instantiating the browser")]
        public string? DriverId { get; set; }
        internal string? GetDriverId(ActivityExecutionContext context)
        {
            if (DriverId != default)
                return DriverId;
            else
            {
                var activities = context.WorkflowInstance.ActivityData as IDictionary<string, IDictionary<string, object>>;
                if (activities == default)
                    return default;
                foreach (var activity in activities)
                {
                    foreach(var variable in activity.Value)
                    {
                        if (variable.Key == RpaWebConventions.DriverIdKey)
                        {
                            DriverId = variable.Value as string;
                            return DriverId;
                        }
                    }
                }
                return default;
            }
        }
        public WebActivity(IServiceProvider sp)
        {
            _factory = sp.GetRequiredService<IBrowserFactory>();
            _options = sp.GetRequiredService<IOptions<RpaWebOptions>>().Value;
        }
    }
}