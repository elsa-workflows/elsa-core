using Elsa.JavaScript.Models;

namespace Elsa.JavaScript.Services;

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