using System.Text.Encodings.Web;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Liquid.Contracts;
using Elsa.Liquid.Notifications;
using Elsa.Liquid.Options;
using Elsa.Mediator.Contracts;
using Fluid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.Liquid.Services;

/// <inheritdoc />
public class LiquidTemplateManager : ILiquidTemplateManager
{
    private readonly LiquidParser _parser;
    private readonly IMemoryCache _memoryCache;
    private readonly INotificationSender _notificationSender;
    private readonly FluidOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LiquidTemplateManager(LiquidParser parser, IMemoryCache memoryCache, INotificationSender notificationSender, IOptions<FluidOptions> options, IServiceProvider serviceProvider)
    {
        _parser = parser;
        _memoryCache = memoryCache;
        _notificationSender = notificationSender;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string?> RenderAsync(string template, ExpressionExecutionContext expressionExecutionContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(template))
            return default!;

        var result = GetCachedTemplate(template);
        var templateContext = await CreateTemplateContextAsync(expressionExecutionContext, cancellationToken);
        templateContext.AddFilters(_options, expressionExecutionContext.ServiceProvider);

        return await result.RenderAsync(templateContext, HtmlEncoder.Default);
    }

    private IFluidTemplate GetCachedTemplate(string source)
    {
        var result = _memoryCache.GetOrCreate(
            source,
            e =>
            {
                if (!TryParse(source, out var parsed, out var error))
                {
                    error = "{% raw %}\n" + error + "\n{% endraw %}";
                    TryParse(error, out parsed, out error);

                    e.SetSlidingExpiration(TimeSpan.FromMilliseconds(100));
                    return parsed;
                }

                // TODO: add signal based cache invalidation.
                e.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                return parsed;
            });
        return result!;
    }

    /// <inheritdoc />
    public bool Validate(string template, out string error) => TryParse(template, out _, out error);

    private bool TryParse(string template, out IFluidTemplate result, out string error) => _parser.TryParse(template, out result, out error);

    private async Task<TemplateContext> CreateTemplateContextAsync(ExpressionExecutionContext expressionExecutionContext, CancellationToken cancellationToken)
    {
        var context = new TemplateContext(expressionExecutionContext, new TemplateOptions());
        await _notificationSender.SendAsync(new RenderingLiquidTemplate(context, expressionExecutionContext), cancellationToken);
        context.SetValue("ExpressionExecutionContext", expressionExecutionContext);
        return context;
    }
}