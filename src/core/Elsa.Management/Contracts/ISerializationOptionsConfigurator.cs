using System.Text.Json;

namespace Elsa.Management.Contracts;

public interface ISerializationOptionsConfigurator
{
    void Configure(JsonSerializerOptions options);
}