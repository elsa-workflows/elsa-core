using Elsa.Scripting.Liquid.Options;
using Fluid;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.Liquid.Services
{
    public class LiquidParser : FluidParser
    {
        public LiquidParser(IOptions<LiquidOptions> options)
        {
            foreach (var configuration in options.Value.ParserConfiguration) 
                configuration(this);
        }
    }
}