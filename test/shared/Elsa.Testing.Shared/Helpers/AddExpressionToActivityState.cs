using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Testing.Shared.Helpers
{
    public class AddExpressionToActivityState : Activity
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly AssertableActivityState _activityState;

        [ActivityInput] public string Expression { get; set; } = default!;
        [ActivityInput] public string ExpressionSyntax { get; set; } = default!;

        public AddExpressionToActivityState(IExpressionEvaluator evaluator, AssertableActivityState activityState)
        {
            _expressionEvaluator = evaluator;
            _activityState = activityState;
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var expressionResult = await _expressionEvaluator.TryEvaluateAsync<string>(Expression, ExpressionSyntax, context);

            _activityState.Messages.Add(expressionResult.Value ?? "");

            return Done();
        }
    }
}