using System.Text.Json;

namespace Elsa.Workflows.Core.Services;

public interface ISerializationOptionsConfigurator
{
    void Configure(JsonSerializerOptions options);
}