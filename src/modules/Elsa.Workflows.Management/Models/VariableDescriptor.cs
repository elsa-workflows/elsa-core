namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a description of a .NET type that can be used as a workflow variable.
/// </summary>
public record VariableDescriptor(Type Type, string Category, string? Description)
{
    public class VariableDescriptorComparer : IEqualityComparer<VariableDescriptor>
    {
        public bool Equals(VariableDescriptor? x, VariableDescriptor? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Type == y.Type;
        }

        public int GetHashCode(VariableDescriptor obj)
        {
            return obj.Type.GetHashCode();
        }
    }
}