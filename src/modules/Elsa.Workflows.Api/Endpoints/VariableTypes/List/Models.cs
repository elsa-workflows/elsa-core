namespace Elsa.Workflows.Api.Endpoints.VariableTypes.List;

internal class Response
{
    public Response(ICollection<VariableTypeDescriptor> items)
    {
        Items = items;
    }

    public ICollection<VariableTypeDescriptor> Items { get; set; }
}

internal record VariableTypeDescriptor(string TypeName, string DisplayName, string Category, string? Description);