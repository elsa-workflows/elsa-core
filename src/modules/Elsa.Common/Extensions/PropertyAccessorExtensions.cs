using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for working with property accessors.
/// </summary>
public static class PropertyAccessorExtensions
{
    /// <summary>
    /// Sets the value of the property referenced by the expression.
    /// </summary>
    /// <param name="target">The target on which to set the property value.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to set.</param>
    public static void SetPropertyValue(this object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName)!;
        property.SetValue(target, value);
    }
        
    /// <summary>
    /// Sets the value of the property referenced by the expression.
    /// </summary>
    /// <param name="target">The target on which to set the property value.</param>
    /// <param name="expression">The expression.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="T">The type of the object containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    public static void SetPropertyValue<T, TProperty>(this T target, Expression<Func<T, TProperty>> expression, TProperty value)
    {
        var property = expression.GetProperty();

        if (property != null)
            property.SetValue(target, value, null);
    }

    /// <summary>
    /// Gets the value of the property referenced by the expression.
    /// </summary>
    /// <param name="target">The target from which to get the property value.</param>
    /// <param name="expression">The expression.</param>
    /// <typeparam name="T">The type of the object containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The value of the property referenced by the expression.</returns>
    public static TProperty? GetPropertyValue<T, TProperty>(this T target, Expression<Func<T, TProperty>> expression)
    {
        var property = expression.GetProperty();
        return (TProperty?)property?.GetValue(target);
    }

    /// <summary>
    /// Gets the name of the property referenced by the expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <typeparam name="T">The type of the object containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The name of the property referenced by the expression.</returns>
    public static string GetPropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression) => expression.GetProperty()!.Name;

    /// <summary>
    /// Gets the property referenced by the expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <typeparam name="T">The type of the object containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The property referenced by the expression.</returns>
    public static PropertyInfo? GetProperty<T, TProperty>(this Expression<Func<T, TProperty>> expression) =>
        expression.Body is MemberExpression memberExpression
            ? memberExpression.Member as PropertyInfo
            : expression.Body is UnaryExpression unaryExpression
                ? unaryExpression.Operand is MemberExpression unaryMemberExpression
                    ? unaryMemberExpression.Member as PropertyInfo
                    : default
                : default;
}