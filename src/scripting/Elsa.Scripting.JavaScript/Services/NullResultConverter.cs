using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class NullResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(evaluationResult is null) return null;
            return wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public NullResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}