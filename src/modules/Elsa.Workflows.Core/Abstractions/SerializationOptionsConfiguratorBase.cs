using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core;

/// <summary>
/// A base class for <see cref="ISerializationOptionsConfigurator"/> implementations.
/// </summary>
public abstract class SerializationOptionsConfiguratorBase : ISerializationOptionsConfigurator
{
    /// <inheritdoc />
    public virtual void Configure(JsonSerializerOptions options)
    {
    }

    /// <inheritdoc />
    public virtual IEnumerable<Action<JsonTypeInfo>> GetModifiers()
    {
        yield break;
    }
}