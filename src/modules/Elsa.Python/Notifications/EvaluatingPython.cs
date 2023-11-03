using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Microsoft.Scripting.Hosting;

namespace Elsa.Python.Notifications;

/// <summary>
/// This notification is published every time a Python expression is about to be evaluated, giving subscribers a chance to modify the Python engine.
/// </summary>
public record EvaluatingPython(ScriptEngine Engine, ScriptScope ScriptScope, ExpressionExecutionContext Context) : INotification;