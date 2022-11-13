using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

public class Response
{
    public Response(ICollection<ActivityDescriptor> items)
    {
        Items = items;
    }

    public ICollection<ActivityDescriptor> Items { get; set; }
}