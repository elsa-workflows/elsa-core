using Elsa.Common.Entities;

namespace Elsa.Agents.Management;

public class ApiKeyDefinition : Entity
{
    public string Name { get; set; } = default!;
    public string Value { get; set; } = default!;

    public ApiKeyConfig ToApiKeyConfig()
    {
        return new ApiKeyConfig
        {
            Name = Name,
            Value = Value
        };
    }
}