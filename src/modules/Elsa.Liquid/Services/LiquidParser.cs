using Elsa.Liquid.Options;
using Fluid;
using Microsoft.Extensions.Options;

namespace Elsa.Liquid.Services;

public class LiquidParser : FluidParser
{
    public LiquidParser(IOptions<FluidOptions> options)
    {
        foreach (var configuration in options.Value.ParserConfiguration) 
            configuration(this);
    }
}