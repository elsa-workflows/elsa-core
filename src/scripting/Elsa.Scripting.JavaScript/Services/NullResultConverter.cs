using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public class NullResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult _wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(evaluationResult is null) return null;
            return _wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        public NullResultConverter(IConvertsJintEvaluationResult wrapped)
        {
            this._wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }
    }
}