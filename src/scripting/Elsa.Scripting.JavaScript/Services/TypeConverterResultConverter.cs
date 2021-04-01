using System;
using System.ComponentModel;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeConverterResultConverter : IConvertsJintEvaluationResult
    {
        readonly IConvertsJintEvaluationResult wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            var converter = TypeDescriptor.GetConverter(evaluationResult);

            if (converter.CanConvertTo(desiredType))
                return converter.ConvertTo(evaluationResult, desiredType);

            return wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public TypeConverterResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}