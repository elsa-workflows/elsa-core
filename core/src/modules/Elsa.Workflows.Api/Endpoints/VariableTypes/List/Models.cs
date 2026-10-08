namespace Elsa.Workflows.Api.Endpoints.VariableTypes.List;

internal class Response(ICollection<VariableTypeDescriptor> items)
{
    public ICollection<VariableTypeDescriptor> Items { get; set; } = items;
}

internal record VariableTypeDescriptor(string TypeName, string DisplayName, string Category, string? Description);