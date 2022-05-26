using System.Text.Encodings.Web;
using Elsa.Expressions.Models;
using Elsa.Liquid.Notifications;
using Elsa.Liquid.Services;
using Elsa.Mediator.Services;
using Fluid;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Liquid.Implementations
{
    public class LiquidTemplateManager : ILiquidTemplateManager
    {
        private readonly LiquidParser _parser;
        private readonly IMemoryCache _memoryCache;
        private readonly IEventPublisher _eventPublisher;

        public LiquidTemplateManager(LiquidParser parser, IMemoryCache memoryCache, IEventPublisher eventPublisher)
        {
            _parser = parser;
            _memoryCache = memoryCache;
            _eventPublisher = eventPublisher;
        }

        public async Task<string?> RenderAsync(string template, ExpressionExecutionContext expressionExecutionContext, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template))
                return default!;

            var result = GetCachedTemplate(template);

            var templateContext =  await CreateTemplateContextAsync(expressionExecutionContext, cancellationToken);

            return await result.RenderAsync(templateContext, HtmlEncoder.Default);
        }

        private IFluidTemplate GetCachedTemplate(string source)
        {
            var result = _memoryCache.GetOrCreate(
                source,
                e =>
                {
                    if (!TryParse(source, out var parsed, out var error))
                        TryParse(error, out parsed, out error);

                    // TODO: add signal based cache invalidation.
                    e.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                    return parsed;
                });
            return result;
        }

        public bool Validate(string template, out string error) => TryParse(template, out _, out error);

        private bool TryParse(string template, out IFluidTemplate result, out string error) => _parser.TryParse(template, out result, out error);

        private async Task<TemplateContext> CreateTemplateContextAsync(ExpressionExecutionContext expressionExecutionContext, CancellationToken cancellationToken)
        {
            var context = new TemplateContext(expressionExecutionContext, new TemplateOptions());
            await _eventPublisher.PublishAsync(new RenderingLiquidTemplate(context, expressionExecutionContext), cancellationToken);
            context.SetValue("ExpressionExecutionContext", expressionExecutionContext);
            return context;
        }
    }
}