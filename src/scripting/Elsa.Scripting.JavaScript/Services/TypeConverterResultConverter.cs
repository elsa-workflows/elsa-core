using System;
using System.ComponentModel;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeConverterResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if (desiredType == typeof(object))
                return evaluationResult;
            
            var converter = TypeDescriptor.GetConverter(evaluationResult!);

            if (converter.CanConvertTo(desiredType))
                return converter.ConvertTo(evaluationResult!, desiredType);

            var targetConverter = TypeDescriptor.GetConverter(desiredType);

            if (targetConverter.CanConvertFrom(evaluationResult!.GetType()))
                return targetConverter.ConvertFrom(evaluationResult!);

            return wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public TypeConverterResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}