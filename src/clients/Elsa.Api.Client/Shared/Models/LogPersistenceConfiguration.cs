using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Enums;

namespace Elsa.Api.Client.Shared.Models;

public class LogPersistenceConfiguration
{
    public LogPersistenceEvaluationMode EvaluationMode { get; set; }
    public string? StrategyType { get; set; }
    public Expression? Expression { get; set; }
}