using Elsa.CSharp.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.CSharp.Providers;

internal class CSharpExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "CSharp";

    public ValueTask<IEnumerable<ExpressionDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var descriptor = CreateCSharpDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionDescriptor>>(new[] { descriptor });
    }

    private static ExpressionDescriptor CreateCSharpDescriptor() => new()
    {
        Type = TypeName,
        DisplayName = "C#",
        HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<CSharpExpressionHandler>
    };
}