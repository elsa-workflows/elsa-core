using Elsa.Expressions.Models;

namespace Elsa.Workflows.Runtime;

public class LogPersistenceConfiguration
{
    public LogPersistenceEvaluationMode EvaluationMode { get; set; }
    public string? StrategyType { get; set; }
    public Expression? Expression { get; set; }
}