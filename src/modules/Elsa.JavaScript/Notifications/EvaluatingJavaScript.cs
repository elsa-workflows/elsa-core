using Elsa.Expressions.Models;
using Elsa.Mediator.Services;
using Jint;

namespace Elsa.JavaScript.Notifications;

public record EvaluatingJavaScript(Engine Engine, ExpressionExecutionContext Context) : INotification;