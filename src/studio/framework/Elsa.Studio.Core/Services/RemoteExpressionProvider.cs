using Elsa.Api.Client.Resources.Scripting.Contracts;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides available expression descriptors from a remote API.
/// </summary>
public class RemoteExpressionProvider : IExpressionProvider
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteExpressionProvider"/> class.
    /// </summary>
    public RemoteExpressionProvider(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ExpressionDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var api = await _backendApiClientProvider.GetApiAsync<IExpressionDescriptorsApi>(cancellationToken);
        var response = await api.ListAsync(cancellationToken);
        return response.Items;
    }
}