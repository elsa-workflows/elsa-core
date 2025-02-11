namespace Elsa.Workflows.Api.Endpoints.StorageDrivers.List;

public class Response(ICollection<StorageDriverDescriptor> items)
{
    public ICollection<StorageDriverDescriptor> Items  { get; set; } = items;
}

public record StorageDriverDescriptor(string TypeName, string DisplayName, double Priority = 0, bool Deprecated = false);