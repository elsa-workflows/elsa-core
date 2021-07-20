using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class PlainObjectResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult _wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if (evaluationResult == null)
                return null;
            
            if(desiredType == typeof(object))
                return evaluationResult;

            if (evaluationResult.GetType() == desiredType || desiredType.IsInstanceOfType(evaluationResult))
                return evaluationResult;

            return _wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public PlainObjectResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this._wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}