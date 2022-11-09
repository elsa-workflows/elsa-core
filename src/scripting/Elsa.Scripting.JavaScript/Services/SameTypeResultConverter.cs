using System;

namespace Elsa.Scripting.JavaScript.Services;

public class SameTypeResultConverter : IConvertsJintEvaluationResult
{
    private readonly IConvertsJintEvaluationResult _wrapped;

    public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
    {
        if(evaluationResult?.GetType() == desiredType) return evaluationResult;
        return _wrapped.ConvertToDesiredType(evaluationResult, desiredType);
    }

    public SameTypeResultConverter(IConvertsJintEvaluationResult wrapped)
    {
        _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
    }
}