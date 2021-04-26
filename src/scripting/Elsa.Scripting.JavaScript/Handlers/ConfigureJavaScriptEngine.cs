using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services;
using Elsa.Services.Models;
using Jint;
using Jint.Runtime.Interop;
using MediatR;
using Microsoft.Extensions.Configuration;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IConfiguration _configuration;

        public ConfigureJavaScriptEngine(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var activityContext = notification.ActivityExecutionContext;
            var workflowExecutionContext = activityContext.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var engine = notification.Engine;

            // Global functions.
            engine.SetValue("guid", (Func<string>) (() => Guid.NewGuid().ToString()));
            engine.SetValue("setVariable", (Action<string, object>) ((name, value) => activityContext.SetVariable(name, value)));
            engine.SetValue("getVariable", (Func<string, object?>) (name => activityContext.GetVariable(name)));
            engine.SetValue("getConfig", (Func<string, object?>) (name => _configuration.GetSection(name).Value));
            engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>) (string.IsNullOrWhiteSpace));
            engine.SetValue("isNullOrEmpty", (Func<string, bool>) (string.IsNullOrEmpty));
            engine.SetValue("getWorkflowDefinitionIdByName", (Func<string, string?>) (name => GetWorkflowDefinitionIdByName(activityContext, name)));
            engine.SetValue("getWorkflowDefinitionIdByTag", (Func<string, string?>) (tag => GetWorkflowDefinitionIdByTag(activityContext, tag)));

            // Global variables.
            engine.SetValue("input", activityContext.Input);
            engine.SetValue("correlationId", workflowInstance.CorrelationId);
            engine.SetValue("currentCulture", CultureInfo.InvariantCulture);
            engine.SetValue("workflowContext", activityContext.GetWorkflowContext());

            // NodaTime types.
            RegisterType<Instant>(engine);
            RegisterType<Duration>(engine);
            RegisterType<Period>(engine);
            RegisterType<LocalDate>(engine);
            RegisterType<LocalTime>(engine);
            RegisterType<LocalDateTime>(engine);

            var variables = workflowExecutionContext.GetMergedVariables();

            // Add workflow variables.
            foreach (var variable in variables.Data)
                engine.SetValue(variable.Key, variable.Value);

            // Add activity outputs.
            foreach (var activity in workflowBlueprint.Activities.Where(x => x.Name is not null and not "" && workflowInstance.ActivityOutput.ContainsKey(x.Id)))
            {
                var output = new { Output = workflowInstance.ActivityOutput[activity.Id!] };
                engine.SetValue(activity.Name, output);
            }

            return Task.CompletedTask;
        }

        private string? GetWorkflowDefinitionIdByTag(ActivityExecutionContext activityExecutionContext, string tag) => GetWorkflowDefinitionId(activityExecutionContext, x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase));
        private string? GetWorkflowDefinitionIdByName(ActivityExecutionContext activityExecutionContext, string name) => GetWorkflowDefinitionId(activityExecutionContext, x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        
        private string? GetWorkflowDefinitionId(ActivityExecutionContext activityExecutionContext, Func<IWorkflowBlueprint, bool> filter)
        {
            var workflowRegistry = activityExecutionContext.GetService<IWorkflowRegistry>();
            var workflowBlueprint = workflowRegistry.FindAsync(filter).Result;
            return workflowBlueprint?.Id;
        }

        private void RegisterType<T>(Engine engine) => engine.SetValue(typeof(T).Name, TypeReference.CreateTypeReference(engine, typeof(T)));
    }
}