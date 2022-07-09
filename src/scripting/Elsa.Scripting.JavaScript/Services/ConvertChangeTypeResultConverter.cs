using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ConvertChangeTypeResultConverter : IConvertsJintEvaluationResult
    {
        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            var underlyingType = Nullable.GetUnderlyingType(desiredType) ?? desiredType;
            return Convert.ChangeType(evaluationResult, underlyingType);
        }
    }
}