using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Scripting.Liquid.Options;
using Fluid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.Liquid.Services
{
    public class LiquidTemplateManager : ILiquidTemplateManager
    {
        private readonly LiquidParser _parser;
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceProvider _serviceProvider;
        private readonly LiquidOptions _options;

        public LiquidTemplateManager(LiquidParser parser, IMemoryCache memoryCache, IOptions<LiquidOptions> options, IServiceProvider serviceProvider)
        {
            _parser = parser;
            _memoryCache = memoryCache;
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public async Task<string?> RenderAsync(string source, TemplateContext context, TextEncoder encoder)
        {
            if (string.IsNullOrWhiteSpace(source))
                return default!;

            context.AddFilters(_options, _serviceProvider);
            var result = GetCachedTemplate(source);

            return await result.RenderAsync(context, encoder);
        }

        private IFluidTemplate GetCachedTemplate(string source)
        {
            string error;

            var result = _memoryCache.GetOrCreate(
                source,
                e =>
                {
                    if (!TryParse(source, out var parsed, out error))
                        TryParse(error, out parsed, out error);

                    e.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                    return parsed;
                });
            return result;
        }

        public bool Validate(string template, out string error) => TryParse(template, out _, out error);
        
        private bool TryParse(string template, out IFluidTemplate result, out string error) => _parser.TryParse(template, out result, out error);
    }
}