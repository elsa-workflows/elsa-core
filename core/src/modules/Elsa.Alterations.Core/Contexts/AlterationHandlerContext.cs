using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Contexts;

/// <summary>
/// Provides contextual information about an alteration.
/// </summary>
public class AlterationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationContext"/> class.
    /// </summary>
    public AlterationContext(
        IAlteration alteration,
        WorkflowExecutionContext workflowExecutionContext,
        AlterationLog log,
        CancellationToken cancellationToken)
    {
        Alteration = alteration;
        WorkflowExecutionContext = workflowExecutionContext;
        AlterationLog = log;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The alteration being handled.
    /// </summary>
    public IAlteration Alteration { get; }

    /// <summary>
    /// A workflow execution context of the workflow instance being altered. This offers maximum flexibility for altering the workflow state.
    /// </summary>
    public WorkflowExecutionContext WorkflowExecutionContext { get; set; }

    /// <summary>
    /// The workflow of the workflow instance being altered.
    /// </summary>
    public Workflow Workflow => WorkflowExecutionContext.Workflow;

    /// <summary>
    /// The cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// The service provider.
    /// </summary>
    public IServiceProvider ServiceProvider => WorkflowExecutionContext.ServiceProvider;

    /// <summary>
    /// The alteration log.
    /// </summary>
    public AlterationLog AlterationLog { get; }

    /// <summary>
    /// A flag indicating whether the alteration has succeeded.
    /// </summary>
    public bool HasSucceeded { get; private set; }

    /// <summary>
    /// A flag indicating whether the alteration has failed.
    /// </summary>
    public bool HasFailed { get; private set; }

    /// <summary>
    /// An optional action to be executed when the alteration is committed. Set this to perform permanent side effects such as deleting records form the database.
    /// </summary>
    public Func<Task>? CommitAction { get; set; }

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="eventName">The event name to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="logLevel">The log level.</param>
    public void Log(string eventName, string message, LogLevel logLevel = LogLevel.Information)
    {
        AlterationLog.Add(message, logLevel, eventName);
    }

    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed()
    {
        Succeed($"{Alteration.GetType().Name} succeeded");
    }

    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed(Func<Task> commitAction)
    {
        Succeed();
        CommitAction = commitAction;
    }

    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed(Action commitAction)
    {
        Succeed();
        CommitAction = () =>
        {
            commitAction();
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed(string message)
    {
        HasSucceeded = true;
        Log($"Alteration {Alteration.GetType().Name} succeeded", message);
    }

    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed(string message, Func<Task> commitAction)
    {
        Succeed(message);
        CommitAction = commitAction;
    }
    
    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed(string message, Action commitAction)
    {
        Succeed(message);
        CommitAction = () =>
        {
            commitAction();
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Marks the alteration as failed.
    /// </summary>
    /// <param name="message">An optional message.</param>
    public void Fail(string? message = null)
    {
        HasFailed = true;
        Log($"Alteration {Alteration.GetType().Name} failed", message ?? $"{Alteration.GetType().Name} failed", LogLevel.Error);
    }
}
