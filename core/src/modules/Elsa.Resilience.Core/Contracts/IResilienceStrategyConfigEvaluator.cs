using Elsa.Expressions.Models;
using Elsa.Resilience.Models;

namespace Elsa.Resilience;

public interface IResilienceStrategyConfigEvaluator
{
    Task<IResilienceStrategy?> EvaluateAsync(ResilienceStrategyConfig? config, ExpressionExecutionContext context, CancellationToken cancellationToken = default);
}