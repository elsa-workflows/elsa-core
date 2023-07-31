using Elsa.Expressions.Models;

namespace Elsa.Expressions.Contracts;

/// <summary>
/// A registry for expression syntaxes.
/// </summary>
public interface IExpressionSyntaxRegistry
{
    /// <summary>
    /// Adds a descriptor to the registry.
    /// </summary>
    /// <param name="descriptor">The descriptor to add.</param>
    void Add(ExpressionSyntaxDescriptor descriptor);
    
    /// <summary>
    /// Adds many descriptors to the registry.
    /// </summary>
    /// <param name="descriptors">The descriptors to add.</param>
    void AddMany(IEnumerable<ExpressionSyntaxDescriptor> descriptors);
    
    /// <summary>
    /// Lists all descriptors in the registry.
    /// </summary>
    /// <returns>A list of descriptors.</returns>
    IEnumerable<ExpressionSyntaxDescriptor> ListAll();
    
    /// <summary>
    /// Finds a descriptor matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A descriptor or null if none was found.</returns>
    ExpressionSyntaxDescriptor? Find(Func<ExpressionSyntaxDescriptor, bool> predicate);
    
    /// <summary>
    /// Finds a descriptor matching the specified syntax.
    /// </summary>
    /// <param name="syntax">The syntax.</param>
    /// <returns>A descriptor or null if none was found.</returns>
    ExpressionSyntaxDescriptor? Find(string syntax);
}