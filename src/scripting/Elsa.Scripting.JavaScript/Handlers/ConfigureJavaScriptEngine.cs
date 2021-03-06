using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using Jint;
using Jint.Runtime.Interop;
using MediatR;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
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
            
            // Global variables.
            engine.SetValue("input", activityContext.Input);
            engine.SetValue("correlationId", workflowInstance.CorrelationId);
            engine.SetValue("currentCulture", CultureInfo.InvariantCulture);
            
            // NodaTime types.
            RegisterType<Instant>(engine);
            RegisterType<Duration>(engine);
            RegisterType<Period>(engine);
            RegisterType<LocalDate>(engine);
            RegisterType<LocalTime>(engine);
            RegisterType<LocalDateTime>(engine);

            var variables = workflowExecutionContext.WorkflowInstance.Variables;

            // Add workflow variables.
            foreach (var variable in variables.Data)
                engine.SetValue(variable.Key, variable.Value);

            // Add activity outputs.
            foreach (var activity in workflowBlueprint.Activities.Where(x => x.Name is not null and not "" && workflowInstance.ActivityOutput.ContainsKey(x.Name))) 
                engine.SetValue(activity.Name, workflowInstance.ActivityOutput[activity.Name!]);

            return Task.CompletedTask;
        }
        
        private void RegisterType<T>(Engine engine) => engine.SetValue(typeof(T).Name, TypeReference.CreateTypeReference(engine, typeof(T)));
    }
}