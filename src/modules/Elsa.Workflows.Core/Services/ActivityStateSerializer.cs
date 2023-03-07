using System.Text.Json;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityStateSerializer : IActivityStateSerializer
{
    private readonly IEnumerable<ISerializationProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityStateSerializer"/> class.
    /// </summary>
    public ActivityStateSerializer(IEnumerable<ISerializationProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc />
    public async Task<JsonElement> SerializeAsync(object? value, CancellationToken cancellationToken = default)
    {
        var serializer = _providers.Where(x => x.Supports(value)).OrderByDescending(x => x.Priority).First();
        return await serializer.SerializeAsync(value, cancellationToken);
    }
}