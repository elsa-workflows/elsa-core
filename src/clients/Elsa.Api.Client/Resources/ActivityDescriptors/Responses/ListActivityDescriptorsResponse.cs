using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Api.Client.Resources.ActivityDescriptors.Responses;

/// <summary>
/// Represents a response from listing activity descriptors.
/// </summary>
/// <param name="Items">The activity descriptors.</param>
/// <param name="Count">The total number of activity descriptors.</param>
public record ListActivityDescriptorsResponse(ICollection<ActivityDescriptor> Items, int Count);