using Elsa.Api.Client.Resources.OutputConverters.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides output converter descriptors that are compatible with declared binding types.
/// </summary>
public interface IOutputConverterService
{
    /// <summary>
    /// Gets the output converters compatible with the specified declared types.
    /// </summary>
    Task<ICollection<OutputConverterDescriptor>> GetOutputConvertersAsync(
        string sourceType,
        string destinationType,
        CancellationToken cancellationToken = default);
}
