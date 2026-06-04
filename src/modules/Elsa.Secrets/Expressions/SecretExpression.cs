using Elsa.Expressions.Models;

namespace Elsa.Secrets.Expressions;

/// <summary>
/// Creates Secret expressions that store references to named secrets.
/// </summary>
public static class SecretExpression
{
    /// <summary>
    /// The Secret expression type name.
    /// </summary>
    public const string TypeName = "Secret";

    /// <summary>
    /// Creates a Secret expression for the specified reference.
    /// </summary>
    public static Expression Create(SecretReference reference) => new(TypeName, reference);
}
