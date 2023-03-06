using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Jint;

namespace Elsa.JavaScript.Notifications;

/// <summary>
/// This notification is published every time a JavaScript expression is about to be evaluated.
/// It gives subscribers a chance to configure the <see cref="Engine"/> with additional functions and variables.
/// </summary>
public record EvaluatingJavaScript(Engine Engine, ExpressionExecutionContext Context) : INotification;