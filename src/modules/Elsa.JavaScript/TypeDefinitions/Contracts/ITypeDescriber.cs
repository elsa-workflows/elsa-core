using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Contracts;

/// <summary>
/// Returns a <see cref="TypeDefinition"/> from a given <see cref="Type"/>.
/// </summary>
public interface ITypeDescriber
{
    /// <summary>
    /// Returns a <see cref="TypeDefinition"/> from a given <see cref="Type"/>.
    /// </summary>
    TypeDefinition DescribeType(Type type);
}