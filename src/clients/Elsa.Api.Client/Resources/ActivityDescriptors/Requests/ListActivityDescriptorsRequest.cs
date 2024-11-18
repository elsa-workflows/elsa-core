using Refit;

namespace Elsa.Api.Client.Resources.ActivityDescriptors.Requests;

/// <summary>
/// Represents a request to list activity descriptors.
/// </summary>
public class ListActivityDescriptorsRequest
{
    /// <summary>
    /// Whether to refresh the activity descriptors or not.
    /// </summary>
    [Query] public bool Refresh { get; set; }
}