using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JavaScriptExpressionHandler : IExpressionHandler
    {
        private readonly IJavaScriptService _javaScriptService;
        public const string SyntaxName = "JavaScript";

        public JavaScriptExpressionHandler(IJavaScriptService javaScriptService)
        {
            _javaScriptService = javaScriptService;
        }

        public string Syntax => SyntaxName;

        public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken) =>
            await _javaScriptService.EvaluateAsync(expression, returnType, context, cancellationToken: cancellationToken);
    }
}