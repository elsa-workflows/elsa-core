using Microsoft.Extensions.Configuration;

namespace Elsa.Liquid.Helpers;

public class ConfigurationSectionWrapper
{
    private readonly IConfigurationSection _section;

    public ConfigurationSectionWrapper(IConfigurationSection section)
    {
        _section = section;
    }

    public override string ToString() => _section.Value;

    public ConfigurationSectionWrapper GetSection(string name) => new(_section.GetSection(name));
}