using Elsa.Mediator.Services;
using Elsa.Models;
using Fluid;

namespace Elsa.Liquid.Notifications;

public record RenderingLiquidTemplate(TemplateContext TemplateContext, ExpressionExecutionContext Context) : INotification;