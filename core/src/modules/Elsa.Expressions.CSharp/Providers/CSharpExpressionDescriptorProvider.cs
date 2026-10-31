using Elsa.Expressions.CSharp.Expressions;
using Elsa.Expressions.CSharp.Options;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.CSharp.Providers;

internal class CSharpExpressionDescriptorProvider(IOptions<CSharpOptions> options) : IExpressionDescriptorProvider
{
    private const string TypeName = "CSharp";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "C#",
            IsBrowsable = options.Value.AllowHostCodeExecution,
            Properties = new { MonacoLanguage = "csharp" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<CSharpExpressionHandler>
        };
    }
}
