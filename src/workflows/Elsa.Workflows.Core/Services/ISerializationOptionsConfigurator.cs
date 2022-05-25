using System.Text.Json;

namespace Elsa.Services;

public interface ISerializationOptionsConfigurator
{
    void Configure(JsonSerializerOptions options);
}