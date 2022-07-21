namespace Elsa.Api.Common.Models;

public class EndpointConfiguration
{
    public ICollection<string> Policies { get; } = new List<string>();
}