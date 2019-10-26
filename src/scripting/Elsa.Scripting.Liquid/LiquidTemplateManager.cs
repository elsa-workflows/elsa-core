using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Scripting.Liquid
{
    public class LiquidTemplateManager : ILiquidTemplateManager
    {
        private readonly IMemoryCache memoryCache;

        public LiquidTemplateManager(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public async Task<string> RenderAsync(string source, TemplateContext context, TextEncoder encoder)
        {
            if (string.IsNullOrWhiteSpace(source))
                return default;

            var result = GetCachedTemplate(source);

            return await result.RenderAsync(context, encoder);
        }

        private FluidTemplate GetCachedTemplate(string source)
        {
            IEnumerable<string> errors;

            var result = memoryCache.GetOrCreate(
                source,
                e =>
                {
                    if (!TryParse(source, out var parsed, out errors))
                        TryParse(string.Join(Environment.NewLine, errors), out parsed, out errors);

                    e.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                    return parsed;
                });
            return result;
        }

        public bool Validate(string template, out IEnumerable<string> errors) => TryParse(template, out _, out errors);
        private bool TryParse(string template, out FluidTemplate result, out IEnumerable<string> errors) => FluidTemplate.TryParse(template, true, out result, out errors);
    }
}