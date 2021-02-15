using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class LiteralHandler : IExpressionHandler
    {
        public const string SyntaxName = "Literal";
        public string Syntax => SyntaxName;

        public Task<object?> EvaluateAsync(
            string expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (returnType == typeof(string))
                return Task.FromResult<object?>(expression);

            var converter = TypeDescriptor.GetConverter(returnType);
            var value = converter.CanConvertFrom(typeof(string)) ? converter.ConvertFrom(expression) : default;
            return Task.FromResult(value)!;
        }
    }
}