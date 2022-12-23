namespace Elsa.Workflows.Api.Endpoints.StorageDrivers.List;

public class Response
{
    public Response(ICollection<StorageDriverDescriptor> items)
    {
        Items = items;
    }

    public ICollection<StorageDriverDescriptor> Items  { get; set; }
}

public record StorageDriverDescriptor(string TypeName, string DisplayName);