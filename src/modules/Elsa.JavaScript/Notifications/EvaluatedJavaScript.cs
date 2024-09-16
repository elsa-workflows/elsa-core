using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Jint;

namespace Elsa.JavaScript.Notifications;

/// <summary>
/// This notification is published every time a JavaScript expression has been evaluated.
/// </summary>
public record EvaluatedJavaScript(Engine Engine, ExpressionExecutionContext Context, object? Result) : INotification;