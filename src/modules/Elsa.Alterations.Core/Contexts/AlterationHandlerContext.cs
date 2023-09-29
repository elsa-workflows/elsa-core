using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Contexts;

/// <summary>
/// Provides contextual information about an alteration.
/// </summary>
public class AlterationHandlerContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationHandlerContext"/> class.
    /// </summary>
    public AlterationHandlerContext(
        AlterationPlan plan, 
        IAlteration alteration, 
        WorkflowExecutionContext workflowExecutionContext, 
        IAlterationLog log, 
        IServiceProvider serviceProvider, 
        CancellationToken cancellationToken)
    {
        Plan = plan;
        Alteration = alteration;
        WorkflowExecutionContext = workflowExecutionContext;
        AlterationLog = log;
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The alteration plan being executed.
    /// </summary>
    public AlterationPlan Plan { get; }

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
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The alteration log.
    /// </summary>
    public IAlterationLog AlterationLog { get; }
    
    /// <summary>
    /// A flag indicating whether the alteration has succeeded.
    /// </summary>
    public bool HasSucceeded { get; private set; }
    
    /// <summary>
    /// A flag indicating whether the alteration has failed.
    /// </summary>
    public bool HasFailed { get; private set; }
    
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="logLevel">The log level.</param>
    public void Log(string message, LogLevel logLevel = LogLevel.Information)
    {
        AlterationLog.Append(message, logLevel);
    }

    /// <summary>
    /// Marks the alteration as succeeded.
    /// </summary>
    public void Succeed(string? message = default)
    {
        HasSucceeded = true;
        Log(message ?? "Succeeded", LogLevel.Information);
    }

    /// <summary>
    /// Marks the alteration as failed.
    /// </summary>
    /// <param name="message">An optional message.</param>
    public void Fail(string? message = default)
    {
        HasFailed = true;
        Log(message ?? "Failed", LogLevel.Error);
    }
}