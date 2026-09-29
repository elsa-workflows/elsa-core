using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <summary>
/// A default implementation of <see cref="IServerInformationProvider"/> that returns dummy data.
/// </summary>
public class EmptyServerInformationProvider : IServerInformationProvider
{
    /// <inheritdoc />
    public ValueTask<ServerInformation> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        return new (new ServerInformation("0.0.0"));
    }
}