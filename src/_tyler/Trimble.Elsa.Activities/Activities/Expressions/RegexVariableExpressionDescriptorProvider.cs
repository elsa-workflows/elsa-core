using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Trimble.Elsa.Activities.Activities.Expressions;

/// <summary>
/// Defines a <see cref="TypeName"/> so Elsa will recognize <see cref="RegexVariableExpressionHandler"/>
/// </summary>
public class RegexVariableExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    public const string TypeName = "RegexVariableMatcher";
    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "Regex Variable Matcher",
            Properties = new { MonacoLanguage = "regexvariable" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<RegexVariableExpressionHandler>
        };
    }
}
