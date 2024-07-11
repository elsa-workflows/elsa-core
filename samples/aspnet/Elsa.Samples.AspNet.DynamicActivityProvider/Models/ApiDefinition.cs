namespace Elsa.Samples.AspNet.DynamicActivityProvider.Models;

public record ApiDefinition(string Name, Uri BaseUrl, ICollection<ApiEndpointDefinition> Endpoints);