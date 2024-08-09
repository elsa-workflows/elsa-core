using Refit;

namespace Elsa.Api.Client.Resources.ActivityDescriptors.Requests;

/// Represents a request to list activity descriptors.
public class ListActivityDescriptorsRequest
{
    /// Whether to refresh the activity descriptors or not.
    [Query] public bool Refresh { get; set; }
}