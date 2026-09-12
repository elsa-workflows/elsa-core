using Elsa.Api.Client.Resources.OutputConverters.Models;

namespace Elsa.Api.Client.Resources.OutputConverters.Responses;

/// <summary>
/// Represents a response containing compatible output converter descriptors.
/// </summary>
/// <param name="Items">The compatible output converter descriptors.</param>
public record ListOutputConvertersResponse(ICollection<OutputConverterDescriptor> Items);
