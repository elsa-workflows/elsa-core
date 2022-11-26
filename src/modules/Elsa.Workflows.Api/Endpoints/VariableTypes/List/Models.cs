namespace Elsa.Workflows.Api.Endpoints.VariableTypes.List;

public class Response
{
    public Response(ICollection<VariableDescriptorModel> items)
    {
        Items = items;
    }

    public ICollection<VariableDescriptorModel> Items { get; set; }
}

public record VariableDescriptorModel(string TypeName, string DisplayName, string Category, string? Description);