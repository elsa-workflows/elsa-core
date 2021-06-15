using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class PlainObjectResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(desiredType == typeof(object))
                return evaluationResult;

            if (evaluationResult.GetType() == desiredType || desiredType.IsAssignableFrom(evaluationResult.GetType()))
                return evaluationResult;

            return wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public PlainObjectResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}