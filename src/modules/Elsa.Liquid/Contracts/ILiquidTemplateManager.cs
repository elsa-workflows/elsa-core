using Elsa.Expressions.Models;

namespace Elsa.Liquid.Contracts;

/// <summary>
/// Provides services to render Liquid templates.
/// </summary>
public interface ILiquidTemplateManager
{
    /// <summary>
    /// Renders a Liquid template as a <see cref="string"/>.
    /// </summary>
    Task<string?> RenderAsync(string template, ExpressionExecutionContext expressionExecutionContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a Liquid template.
    /// </summary>
    bool Validate(string template, out string error);
}