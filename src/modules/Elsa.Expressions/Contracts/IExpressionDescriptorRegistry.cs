using Elsa.Expressions.Models;

namespace Elsa.Expressions.Contracts;

/// <summary>
/// A registry for expression syntaxes.
/// </summary>
public interface IExpressionDescriptorRegistry
{
    /// <summary>
    /// Adds a descriptor to the registry.
    /// </summary>
    /// <param name="descriptor">The descriptor to add.</param>
    void Add(ExpressionDescriptor descriptor);
    
    /// <summary>
    /// Adds many descriptors to the registry.
    /// </summary>
    /// <param name="descriptors">The descriptors to add.</param>
    void AddRange(IEnumerable<ExpressionDescriptor> descriptors);
    
    /// <summary>
    /// Lists all descriptors in the registry.
    /// </summary>
    /// <returns>A list of descriptors.</returns>
    IEnumerable<ExpressionDescriptor> ListAll();
    
    /// <summary>
    /// Finds a descriptor matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A descriptor or null if none was found.</returns>
    ExpressionDescriptor? Find(Func<ExpressionDescriptor, bool> predicate);
    
    /// <summary>
    /// Finds a descriptor matching the specified syntax.
    /// </summary>
    /// <param name="type">The syntax.</param>
    /// <returns>A descriptor or null if none was found.</returns>
    ExpressionDescriptor? Find(string type);
}