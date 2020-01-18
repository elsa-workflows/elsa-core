using System;
using Elsa;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Runtime;
using Elsa.Scripting.Liquid.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsa(
            this IServiceCollection services,
            Action<ElsaOptions> configure = null)
        {
            return services
                .AddElsaCore(configure)
                .AddStartupRunner()
                .AddJavaScriptExpressionEvaluator()
                .AddLiquidExpressionEvaluator()
                .AddUserTaskActivities();
        }
    }
}