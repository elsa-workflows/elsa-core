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
            engine.SetValue("transientState", (Func<string, object>) (name => executionContext.TransientState.GetVariable(name)));
            engine.SetValue("lastResult", (Func<string, object>) (name => executionContext.CurrentScope.LastResult?.Value));
            engine.SetValue("correlationId", (Func<object>) (() => executionContext.Workflow.CorrelationId));
            engine.SetValue("currentCulture", (Func<object>) (() => CultureInfo.InvariantCulture));
            engine.SetValue("newGuid", (Func<string>) (() => Guid.NewGuid().ToString()));

            var variables = executionContext.GetVariables();
            var transientState = executionContext.TransientState;

            foreach (var variable in variables)
                engine.SetValue(variable.Key, variable.Value.Value);
            
            foreach (var variable in transientState)
                engine.SetValue(variable.Key, variable.Value.Value);

            foreach (var activity in executionContext.Workflow.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Output != null))
            {
                var expando = new ExpandoObject() as IDictionary<string, object>;

                foreach (var variable in activity.Output)
                    expando[variable.Key] = variable.Value.Value;

                engine.SetValue(activity.Name, expando);
            }

            return Task.CompletedTask;
        }
    }
}