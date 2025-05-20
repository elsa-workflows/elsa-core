using Elsa.Expressions.Models;
using Microsoft.CodeAnalysis.Scripting;

namespace Elsa.Scripting.CSharp.Contexts;

/// <summary>
/// Context for configuring the <see cref="ScriptOptions"/>.
/// </summary>
/// <param name="Script">The script.</param>
/// <param name="ExpressionExecutionContext">The expression execution context.</param>
public record ScriptConfigurationContext(Script Script, ExpressionExecutionContext ExpressionExecutionContext);