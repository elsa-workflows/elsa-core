using System;
using System.ComponentModel;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeConverterResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult _wrapped;

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

            return _wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public TypeConverterResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this._wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}