using System.Text.Json;

namespace Elsa.Workflows.Core.Contracts;

public interface ISerializationOptionsConfigurator
{
    void Configure(JsonSerializerOptions options);
}