using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;
using Flowsharp.Scripting;

namespace Flowsharp.Activities
{
    public class IfElse : Activity
    {
        private readonly IScriptEvaluator scriptEvaluator;

        public IfElse(IScriptEvaluator scriptEvaluator)
        {
            this.scriptEvaluator = scriptEvaluator;
        }

        public ScriptExpression<bool> ConditionExpression { get; set; }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            var result = await scriptEvaluator.EvaluateAsync(ConditionExpression, workflowContext, cancellationToken);
            
            return ActivateEndpoint(result ? "True" : "False");
        }
    }
}