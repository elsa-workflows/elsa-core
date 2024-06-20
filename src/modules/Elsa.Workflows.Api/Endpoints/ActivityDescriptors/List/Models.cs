using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class Response(ICollection<ActivityDescriptor> items)
{
    public ICollection<ActivityDescriptor> Items { get; set; } = items;
    public int Count => Items.Count;
}