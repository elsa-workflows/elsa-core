using System.ComponentModel;
using Elsa.Mediator.Contracts;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Middleware.Activities;

/// <summary>
/// Abstract class used to test activity execution middleware components.
/// It provides a set of standard tests that validate the behavior of the middleware component when executing an activity, such as:
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ActivityInvokerMiddlewareTestsBase<T> : ActivityExecutionMiddlewareTestsBase<T> where T : class, IActivityExecutionMiddleware
{
    protected ActivityInvokerMiddlewareTestsBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }


    [Fact]
    public async Task ActivityExceuteThrows_IncidentsCount_IsOne()
    {
        // Setup 
        _activity.ExecuteThrows = new Exception("EXCEPTION!");

        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        Assert.Single(ExecutionContext.WorkflowExecutionContext.Incidents);
    }

    [Fact]
    public async Task ActivityExceuteThrows_ActivityStatus_IsFaulted()
    {
        // Setup 
        _activity.ExecuteThrows = new Exception("EXCEPTION!");

        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        Assert.Equal(ActivityStatus.Faulted, ExecutionContext.Status);
        Assert.Equal(1, ExecutionContext.AggregateFaultCount);
    }

    [Fact]
    public async Task ActivityExecuteFaults_ActivityStatus_IsFualted()
    {
        // Setup 
        _activity.ExecuteFaults = new Exception("EXCEPTION!");

        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        Assert.Equal(ActivityStatus.Faulted, ExecutionContext.Status);
        Assert.Equal(1, ExecutionContext.AggregateFaultCount);
    }

    [Fact]
    public async Task ActivityCanExceuteThrows_IncidentsCount_IsOne()
    {
        // Setup 
        _activity.CanExecuteThrows = new Exception("EXCEPTION!");

        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        Assert.Single(ExecutionContext.WorkflowExecutionContext.Incidents);
    }

    [Fact]
    public async Task ActivityCanExceuteThrows_ActivityStatus_IsFaulted()
    {
        // Setup 
        _activity.CanExecuteThrows = new Exception("EXCEPTION!");

        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        Assert.Equal(ActivityStatus.Faulted, ExecutionContext.Status);
    }

}
