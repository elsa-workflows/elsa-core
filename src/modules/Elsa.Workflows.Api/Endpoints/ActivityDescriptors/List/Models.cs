using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class Response
{
    public Response(ICollection<ActivityDescriptor> items)
    {
        Items = items;
    }

    public ICollection<ActivityDescriptor> Items { get; set; }
    public int Count => Items.Count;
}