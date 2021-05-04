using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Newtonsoft.Json;

namespace Elsa.Expressions
{
    public class JsonHandler : IExpressionHandler
    {
        public string Syntax => SyntaxNames.Json;

        public Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var value = JsonConvert.DeserializeObject(expression, returnType);
            return Task.FromResult(value)!;
        }
    }
}