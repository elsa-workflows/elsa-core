using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Jint;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface IJavaScriptService
    {
        Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, Action<Engine>? configureEngine = default, CancellationToken cancellationToken = default);
    }
}