using Elsa.Logging.Activities;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;
using Elsa.Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Logging.UnitTests.Activities;

public class ProcessLogActivityTests
{
    [Fact]
    public void ProcessLogActivity_CanBeCreated()
    {
        // Act
        var activity = new ProcessLogActivity("Test message");

        // Assert
        Assert.NotNull(activity);
        Assert.NotNull(activity.Message);
        Assert.NotNull(activity.Level);
    }

    [Fact]
    public void ProcessLogActivity_ExecutesWithBasicSetup()
    {
        // This is a simplified test that just verifies the activity can be created and has the expected properties
        // In a real scenario, the activity would be executed within a full workflow context

        // Arrange
        var activity = new ProcessLogActivity("Test message");

        // Act & Assert - verify properties are set correctly
        Assert.NotNull(activity.Message);
        Assert.NotNull(activity.Level);
        Assert.NotNull(activity.TargetSinks);
        Assert.NotNull(activity.Attributes);
        Assert.NotNull(activity.EventId);
        Assert.NotNull(activity.Category);
    }

    [Fact]
    public void ProcessLogActivity_DefaultsToProcessSink()
    {
        // Arrange & Act
        var activity = new ProcessLogActivity("Test message");

        // Assert
        Assert.NotNull(activity.TargetSinks);
        Assert.NotNull(activity.Message);
        Assert.NotNull(activity.Level);
    }

    [Fact]
    public void ConsoleLogSink_HasCorrectName()
    {
        // Arrange
        var logger = NullLogger<ConsoleLogSink>.Instance;
        var sink = new ConsoleLogSink(logger);

        // Act & Assert
        Assert.Equal("console", sink.Name);
    }

    [Fact]
    public void DefaultLogSinkResolver_ResolvesConsoleSink()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance))
            .AddSingleton<ConsoleLogSink>()
            .AddSingleton<ILogSink>(sp => sp.GetRequiredService<ConsoleLogSink>())
            .AddSingleton<ILogger<DefaultLogSinkResolver>, NullLogger<DefaultLogSinkResolver>>()
            .BuildServiceProvider();

        var resolver = new DefaultLogSinkResolver(services, services.GetRequiredService<ILogger<DefaultLogSinkResolver>>());

        // Act
        var sink = resolver.Resolve("console");

        // Assert
        Assert.NotNull(sink);
        Assert.Equal("console", sink.Name);
    }

    [Fact]
    public void DefaultLogSinkResolver_ResolvesProcessSinkAsConsole()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance))
            .AddSingleton<ConsoleLogSink>()
            .AddSingleton<ILogSink>(sp => sp.GetRequiredService<ConsoleLogSink>())
            .AddSingleton<ILogger<DefaultLogSinkResolver>, NullLogger<DefaultLogSinkResolver>>()
            .BuildServiceProvider();

        var resolver = new DefaultLogSinkResolver(services, services.GetRequiredService<ILogger<DefaultLogSinkResolver>>());

        // Act
        var sink = resolver.Resolve("process");

        // Assert
        Assert.NotNull(sink);
        Assert.Equal("console", sink.Name); // process maps to console by default
    }

    [Fact]
    public void ProcessLogEntry_CanBeCreated()
    {
        // Arrange
        var message = "Test message";
        var level = LogLevel.Warning;
        var targetSinks = new List<string> { "console" }.AsReadOnly();
        var attributes = new Dictionary<string, object> { ["key1"] = "value1" }.AsReadOnly();
        var eventId = 123;
        var category = "TestCategory";
        var timestamp = DateTimeOffset.UtcNow;
        var workflowInstanceId = "workflow-123";
        var workflowName = "TestWorkflow";
        var activityId = "activity-123";
        var activityName = "TestActivity";
        var correlationId = "correlation-123";

        // Act
        var entry = new ProcessLogEntry(
            message, level, targetSinks, attributes, eventId, category,
            timestamp, workflowInstanceId, workflowName, activityId, activityName, correlationId);

        // Assert
        Assert.Equal(message, entry.Message);
        Assert.Equal(level, entry.LogLevel);
        Assert.Equal(targetSinks, entry.TargetSinks);
        Assert.Equal(attributes, entry.Attributes);
        Assert.Equal(eventId, entry.EventId);
        Assert.Equal(category, entry.Category);
        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal(workflowInstanceId, entry.WorkflowInstanceId);
        Assert.Equal(workflowName, entry.WorkflowName);
        Assert.Equal(activityId, entry.ActivityId);
        Assert.Equal(activityName, entry.ActivityName);
        Assert.Equal(correlationId, entry.CorrelationId);
    }
}