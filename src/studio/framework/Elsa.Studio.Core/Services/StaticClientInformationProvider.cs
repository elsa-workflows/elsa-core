using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using JetBrains.Annotations;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides static client information by returning <see cref="ToolVersion.Version"/>.
/// </summary>
[UsedImplicitly]
public class StaticClientInformationProvider : IClientInformationProvider
{
    /// <inheritdoc />
    public ValueTask<ClientInformation> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var version = ToolVersion.Version.ToString();
        return new(new ClientInformation(version));
    }
}