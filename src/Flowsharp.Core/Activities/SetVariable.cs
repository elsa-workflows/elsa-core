using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;
using Flowsharp.Scripting;

namespace Flowsharp.Activities
{
    public class SetVariable : Activity
    {
        private readonly IScriptEvaluator scriptEvaluator;

        public SetVariable(IScriptEvaluator scriptEvaluator)
        {
            this.scriptEvaluator = scriptEvaluator;
        }
   
        public string VariableName { get; set; }
        public ScriptExpression<object> ValueExpression { get; set; }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            var value = await scriptEvaluator.EvaluateAsync(ValueExpression, workflowContext, cancellationToken);
            workflowContext.CurrentScope.SetVariable(VariableName, value);
            return ActivateEndpoint();
        }
    }
}