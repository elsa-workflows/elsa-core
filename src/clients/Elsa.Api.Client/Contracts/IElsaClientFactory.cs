using Elsa.Api.Client.Options;

namespace Elsa.Api.Client.Contracts;

/// <summary>
/// A factory for creating <see cref="IElsaClient"/> instances.
/// </summary>
public interface IElsaClientFactory
{
    /// <summary>
    /// Creates a new <see cref="IElsaClient"/> instance.
    /// </summary>
    IElsaClient CreateClient(Action<ElsaClientOptions> configureOptions);
}