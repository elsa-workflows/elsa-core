using Elsa.Liquid.Options;
using Fluid;
using Microsoft.Extensions.Options;

namespace Elsa.Liquid.Services;

/// <summary>
/// A parser for the Liquid templating engine.
/// </summary>
public class LiquidParser : FluidParser
{
    /// <inheritdoc />
    public LiquidParser(IOptions<FluidOptions> options)
    {
        foreach (var configuration in options.Value.ParserConfiguration) 
            configuration(this);
    }
}