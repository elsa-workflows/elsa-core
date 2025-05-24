using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Expressions.PowerFx.Expressions;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.PowerFx.Providers;

[UsedImplicitly]
internal class PowerFxExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "PowerFx";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "Power Fx",
            Properties = new { MonacoLanguage = "excel" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<PowerFxExpressionHandler>
        };
    }
}