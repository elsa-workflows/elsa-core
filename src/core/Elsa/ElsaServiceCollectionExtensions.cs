using System;
using Elsa;
using Elsa.Activities.ControlFlow.Extensions;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Activities.Workflows.Extensions;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.Extensions.Options;

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
                .AddJavaScriptExpressionEvaluator()
                .AddLiquidExpressionEvaluator()
                .AddControlFlowActivities()
                .AddWorkflowActivities()
                .AddUserTaskActivities();
        }

        public static IServiceCollection WithJavaScriptOptions(this IServiceCollection services, Action<OptionsBuilder<ScriptOptions>> options)
        {
            var scriptOptions = services.AddOptions<ScriptOptions>();
            options(scriptOptions);

            return services;
        }
    }
}