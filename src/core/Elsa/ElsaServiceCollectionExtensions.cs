using System;
using Elsa;
using Elsa.Activities.UserTask.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaServiceCollectionExtensions
    {
        public static IServiceCollection AddElsa(
            this IServiceCollection services,
            Action<ElsaBuilder> configure = null)
        {
            return services
                .AddElsaCore(configure)
                // .AddJavaScriptExpressionEvaluator()
                // .AddLiquidExpressionEvaluator()
                .AddUserTaskActivities();
        }
    }
}