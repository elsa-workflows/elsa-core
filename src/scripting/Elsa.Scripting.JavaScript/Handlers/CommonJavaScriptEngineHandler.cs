using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class CommonJavaScriptEngineHandler : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var executionContext = notification.WorkflowExecutionContext;
            var workflow = executionContext.Workflow;
            var engine = notification.Engine;

            engine.SetValue("input", (Func<string, object>) (name => workflow.Input.GetVariable(name)));
            engine.SetValue("variable", (Func<string, object>) (name => executionContext.CurrentScope.GetVariable(name)));
            engine.SetValue("correlationId", (Func<object>) (() => executionContext.Workflow.CorrelationId));
            engine.SetValue("currentCulture", (Func<object>) (() => CultureInfo.InvariantCulture));
            engine.SetValue("newGuid", (Func<string>) (() => Guid.NewGuid().ToString()));

            var variables = executionContext.GetVariables();

            // Add workflow variables.
            foreach (var variable in variables)
                engine.SetValue(variable.Key, variable.Value.Value);

            // Add activity outputs.
            foreach (var activity in executionContext.Workflow.Blueprint.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Output != null))
            {
                engine.SetValue(activity.Name, activity.Output);
            }

            return Task.CompletedTask;
        }
    }
}