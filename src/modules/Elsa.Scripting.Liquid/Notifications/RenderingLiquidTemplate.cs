using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Fluid;

namespace Elsa.Scripting.Liquid.Notifications;

public record RenderingLiquidTemplate(TemplateContext TemplateContext, ExpressionExecutionContext Context) : INotification;