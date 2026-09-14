using System.ComponentModel;
using System.Collections.Concurrent;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.ComponentTests.Scenarios.HostMethodActivities;

/// <summary>
/// A test host class for HostMethod activity testing.
/// Each public method becomes an activity that can be executed.
/// </summary>
[UsedImplicitly]
[Description("Test Host Method")]
public class TestHostMethod(ILogger<TestHostMethod> logger, TestHostMethodState state)
{
    /// <summary>
    /// Simple method with no parameters or return value
    /// </summary>
    [Activity(Description = "Performs a simple action")]
    public void SimpleAction()
    {
        logger.LogInformation("SimpleAction invoked");
        state.Record("SimpleAction");
    }

    /// <summary>
    /// Method with a single string parameter
    /// </summary>
    public void GreetPerson(string name)
    {
        logger.LogInformation($"Greeting {name}");
        state.Record($"GreetPerson:{name}");
    }

    /// <summary>
    /// Method with multiple parameters
    /// </summary>
    public void AddNumbers(int a, int b)
    {
        var sum = a + b;
        logger.LogInformation($"Adding {a} + {b} = {sum}");
        state.Record($"AddNumbers:{a}+{b}={sum}");
    }

    /// <summary>
    /// Method that returns a value
    /// </summary>
    public string GetMessage(string prefix)
    {
        var message = $"{prefix}: Hello from HostMethod!";
        logger.LogInformation($"GetMessage returned: {message}");
        state.Record($"GetMessage:{prefix}");
        return message;
    }

    /// <summary>
    /// Method that returns an integer
    /// </summary>
    public int Calculate(int x, int y)
    {
        var result = x * y;
        logger.LogInformation($"Calculate: {x} * {y} = {result}");
        state.Record($"Calculate:{x}*{y}={result}");
        return result;
    }

    /// <summary>
    /// Async method that returns a value
    /// </summary>
    public async Task<string> GetAsyncMessage(string text)
    {
        await Task.Delay(10);
        var message = $"Async: {text}";
        logger.LogInformation($"GetAsyncMessage returned: {message}");
        state.Record($"GetAsyncMessage:{text}");
        return message;
    }

    /// <summary>
    /// Method that accepts ActivityExecutionContext
    /// </summary>
    public void UseContext(string data, ActivityExecutionContext context)
    {
        var workflowInstanceId = context.WorkflowExecutionContext.Id;
        logger.LogInformation($"UseContext invoked with data={data}, workflowInstanceId={workflowInstanceId}");
        state.Record($"UseContext:{data}:{workflowInstanceId}");
    }

    /// <summary>
    /// Method that accepts CancellationToken
    /// </summary>
    public async Task ProcessWithCancellation(string item, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        logger.LogInformation($"ProcessWithCancellation completed for: {item}");
        state.Record($"ProcessWithCancellation:{item}");
    }

    /// <summary>
    /// Method that creates a bookmark for workflow suspension
    /// </summary>
    public string CreateBookmark(string bookmarkName, ActivityExecutionContext context)
    {
        logger.LogInformation($"Creating bookmark: {bookmarkName}");
        var bookmark = context.CreateBookmark(ResumeFromBookmark);
        state.Record($"CreateBookmark:{bookmarkName}");
        return context.GenerateBookmarkTriggerToken(bookmark.Id);
    }

    /// <summary>
    /// Private callback method invoked when bookmark is resumed
    /// </summary>
    private ValueTask ResumeFromBookmark(ActivityExecutionContext context)
    {
        logger.LogInformation("Bookmark resumed");
        state.Record("ResumeFromBookmark");
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Method with custom Activity attribute settings
    /// </summary>
    [Activity(
        DisplayName = "Custom Display Name",
        Description = "Custom description for this activity",
        Category = "Custom Category",
        Namespace = "CustomNamespace",
        Type = "CustomType"
    )]
    public void CustomAttributeMethod()
    {
        logger.LogInformation("CustomAttributeMethod invoked");
        state.Record("CustomAttributeMethod");
    }

    /// <summary>
    /// Method with default parameter value
    /// </summary>
    public void WithDefaultValue(string message = "default message")
    {
        logger.LogInformation($"WithDefaultValue: {message}");
        state.Record($"WithDefaultValue:{message}");
    }

    /// <summary>
    /// Method that returns Task (void async)
    /// </summary>
    public async Task AsyncAction(string action)
    {
        await Task.Delay(10);
        logger.LogInformation($"AsyncAction: {action}");
        state.Record($"AsyncAction:{action}");
    }

    /// <summary>
    /// Method with complex return type
    /// </summary>
    public Dictionary<string, object> GetComplexData(string key, string value)
    {
        var data = new Dictionary<string, object>
        {
            [key] = value,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };
        logger.LogInformation($"GetComplexData: {key}={value}");
        state.Record($"GetComplexData:{key}={value}");
        return data;
    }

    /// <summary>
    /// Static method retained solely to verify that host activity discovery excludes static methods.
    /// </summary>
    public static void ClearLog()
    {
    }
}

/// <summary>
/// Host-local invocation state owned by the session-shared component host.
/// </summary>
public sealed class TestHostMethodState
{
    private readonly ConcurrentQueue<string> _invocations = new();

    public IReadOnlyCollection<string> Invocations => [.. _invocations];

    public void Record(string invocation) => _invocations.Enqueue(invocation);
}
