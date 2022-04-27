using System;
using System.ComponentModel;
using System.Data;

namespace Elsa.Scripting.JavaScript.Services;

/// <summary>
/// Converts <see cref="DataSet"/> and <see cref="DataTable"/>.
/// </summary>
public class ListSourceConverter : IConvertsJintEvaluationResult
{
    private readonly IConvertsJintEvaluationResult _wrapped;

    public ListSourceConverter(IConvertsJintEvaluationResult wrapped)
    {
        _wrapped = wrapped;
    }
        
    public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
    {
        var valueToConvert = evaluationResult is IListSource listSource ? listSource.GetList() : evaluationResult;
        return _wrapped.ConvertToDesiredType(valueToConvert, desiredType);
    }
}