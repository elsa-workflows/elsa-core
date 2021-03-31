using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ConvertChangeTypeResultConverter : IConvertsJintEvaluationResult
    {
        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            return Convert.ChangeType(evaluationResult, desiredType);
        }
    }
}