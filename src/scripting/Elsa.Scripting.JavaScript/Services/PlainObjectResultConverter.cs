using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class PlainObjectResultConverter : IConvertsJintEvaluationResult
    {
        readonly IConvertsJintEvaluationResult wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(desiredType == typeof(object))
                return evaluationResult;

            return wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public PlainObjectResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}