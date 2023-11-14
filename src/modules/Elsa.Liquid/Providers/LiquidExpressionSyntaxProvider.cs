using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Liquid.Expressions;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Providers;

/// <summary>
/// Provides Liquid expression descriptors.
/// </summary>
public class LiquidExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    /// <summary>
    /// Gets the name of the expression type.
    /// </summary>
    public const string TypeName = "Liquid";
    private readonly IIdentityGenerator _identityGenerator;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LiquidExpressionDescriptorProvider"/> class.
    /// </summary>
    public LiquidExpressionDescriptorProvider(IIdentityGenerator identityGenerator) => _identityGenerator = identityGenerator;

    /// <inheritdoc />
    public ValueTask<IEnumerable<ExpressionDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var liquidDescriptor = CreateLiquidDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionDescriptor>>(new[] { liquidDescriptor });
    }

    private ExpressionDescriptor CreateLiquidDescriptor() => new()
    {
        Type = TypeName,
        DisplayName = "Liquid",
        Properties = new { MonacoLanguage = "liquid" }.ToDictionary(),
        HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<LiquidExpressionHandler> 
    };
}