using Elsa.Mediator.Services;
using Elsa.Models;
using Jint;

namespace Elsa.Scripting.JavaScript.Notifications;

public record EvaluatingJavaScript(Engine Engine, ExpressionExecutionContext Context) : INotification;