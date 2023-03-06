using Elsa.Liquid.Services;

namespace Elsa.Liquid.Options;

public class FluidOptions
{
    public Dictionary<string, Type> FilterRegistrations { get; }  = new();
    public IList<Action<LiquidParser>> ParserConfiguration { get; } = new List<Action<LiquidParser>>();
    public bool AllowConfigurationAccess { get; set; }
}