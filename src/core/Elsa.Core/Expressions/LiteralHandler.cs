using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class LiteralHandler : IExpressionHandler
    {
        public string Syntax => SyntaxNames.Literal;

        public Task<object?> EvaluateAsync(
            string expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (returnType == typeof(string) || returnType == typeof(object))
                return Task.FromResult<object?>(expression);

            if (string.IsNullOrWhiteSpace(expression))
                return Task.FromResult((object?) null);
            
            var converter = TypeDescriptor.GetConverter(returnType);
            var value = converter.CanConvertFrom(typeof(string)) ? converter.ConvertFrom(expression) : default;
            return Task.FromResult(value)!;
        }
    }
}