namespace Elsa.Api.Client.Resources.ActivityDescriptorOptions.Responses;
/// <summary>
/// Represents a response from get activity descriptors Options.
/// </summary>
/// <param name="Items">The options elements.</param>
public record GetActivityDescriptorOptionsResponse(IDictionary<string,object> Items);