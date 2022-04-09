using System.Text.Json;

namespace Elsa.Contracts;

public interface ISerializationOptionsConfigurator
{
    void Configure(JsonSerializerOptions options);
}