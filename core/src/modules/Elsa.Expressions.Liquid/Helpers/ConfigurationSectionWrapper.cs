using Microsoft.Extensions.Configuration;

namespace Elsa.Expressions.Liquid.Helpers;

public class ConfigurationSectionWrapper(IConfigurationSection section)
{
    public override string ToString() => section.Value!;

    public ConfigurationSectionWrapper GetSection(string name) => new(section.GetSection(name));
}