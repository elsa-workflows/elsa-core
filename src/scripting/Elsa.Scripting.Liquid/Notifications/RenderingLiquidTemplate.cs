using Elsa.Mediator.Contracts;
using Elsa.Models;
using Fluid;

namespace Elsa.Scripting.Liquid.Notifications;

public record RenderingLiquidTemplate(TemplateContext TemplateContext, ExpressionExecutionContext Context) : INotification;