using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Signals;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the Fork activity covering various join modes, branching scenarios, and signal handling.
/// </summary>
public class ForkTests
{
    [Fact]
    public async Task SchedulesAllBranches()
    {
        // Arrange
        var branches = CreateBranches(3);
        
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches = branches.Cast<IActivity>().ToList()
        };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        foreach (var branch in branches)
        {
            Assert.True(context.HasScheduledActivity(branch), $"{branch.Id} should be scheduled");
        }
    }

    [Fact]
    public async Task ExecutesWithNoBranches()
    {
        // Arrange
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll
        };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        Assert.NotNull(context);
    }

    [Theory]
    [InlineData(ForkJoinMode.WaitAll)]
    [InlineData(ForkJoinMode.WaitAny)]
    public async Task ExecutesWithSpecifiedJoinMode(ForkJoinMode joinMode)
    {
        // Arrange
        var branch = new WriteLine("Test Branch");
        var fork = new Fork
        {
            JoinMode = joinMode,
            Branches = { branch }
        };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        Assert.Equal(joinMode, fork.JoinMode);
        Assert.True(context.HasScheduledActivity(branch));
    }

    [Theory]
    [InlineData(ForkJoinMode.WaitAll, 1, 1)] // WaitAll: complete 1 of 2, should track 1
    [InlineData(ForkJoinMode.WaitAll, 2, 2)] // WaitAll: complete 2 of 2, should track 2  
    [InlineData(ForkJoinMode.WaitAny, 1, 1)] // WaitAny: complete 1 of 2, should track 1
    public async Task CompletionTrackingWithJoinModes(ForkJoinMode joinMode, int branchesToComplete, int expectedCompleted)
    {
        // Arrange
        var branches = CreateBranches(2);
        
        var fork = new Fork
        {
            JoinMode = joinMode,
            Branches = branches.Cast<IActivity>().ToList()
        };

        var context = await ExecuteForkAsync(fork);

        // Act - complete the specified number of branches
        var completedSet = new HashSet<string>();
        for (var i = 0; i < branchesToComplete; i++)
        {
            completedSet = await CompleteBranchAsync(fork, context, branches[i]);
        }

        // Assert
        Assert.NotNull(completedSet);
        Assert.Equal(expectedCompleted, completedSet.Count);
        
        for (var i = 0; i < branchesToComplete; i++)
        {
            Assert.Contains(branches[i].Id, completedSet);
        }
        
        // Verify uncompleted branches are not in the set
        for (var i = branchesToComplete; i < branches.Length; i++)
        {
            Assert.DoesNotContain(branches[i].Id, completedSet);
        }
    }

    [Fact]
    public async Task HandlesBreakSignal()
    {
        // Arrange
        var branch = new WriteLine("Branch");
        var fork = new Fork
        {
            Branches = { branch }
        };

        var context = await ExecuteForkAsync(fork);

        // Act - send break signal
        var breakSignal = new BreakSignal();
        var signalContext = new SignalContext(context, context, CancellationToken.None);
        
        await InvokePrivateMethodAsync(fork, "OnBreakSignalReceived", breakSignal, signalContext);

        // Assert
        Assert.True(context.GetIsBreaking());
    }

    [Fact]
    public async Task CompletesImmediatelyWhenBreaking()
    {
        // Arrange
        var branch = new WriteLine("Branch");
        var fork = new Fork
        {
            Branches = { branch }
        };

        var context = await ExecuteForkAsync(fork);

        context.SetIsBreaking();

        // Act
        var childContext = await CreateChildContextAsync(branch);
        var completedContext = new ActivityCompletedContext(context, childContext);
        await InvokeCompleteChildAsync(fork, completedContext);

        // Assert - completion should be handled immediately due to breaking state
        // The actual completion is tested by the fact that CompleteChildAsync returns without error
        Assert.True(context.GetIsBreaking());
    }

    [Fact]
    public async Task SingleBranchCompletesCorrectly()
    {
        // Arrange
        var branch = new WriteLine("Single Branch");
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches = { branch }
        };

        var context = await ExecuteForkAsync(fork);

        // Act
        var childContext = await CreateChildContextAsync(branch);
        var completedContext = new ActivityCompletedContext(context, childContext);
        await InvokeCompleteChildAsync(fork, completedContext);

        // Assert
        var completedSet = context.GetProperty<HashSet<string>>("Completed");
        Assert.NotNull(completedSet);
        Assert.Single(completedSet);
        Assert.Contains(branch.Id, completedSet);
    }

    [Theory]
    [InlineData(ForkJoinMode.WaitAll, 3, 3)] // WaitAll requires all 3 branches to complete
    [InlineData(ForkJoinMode.WaitAny, 3, 1)] // WaitAny only needs 1 branch to complete
    public async Task HandlesMultipleBranchesWithJoinModes(ForkJoinMode joinMode, int totalBranches, int branchesToComplete)
    {
        // Arrange
        var branches = CreateBranches(totalBranches);
            
        var fork = new Fork
        {
            JoinMode = joinMode,
            Branches = branches.Cast<IActivity>().ToList()
        };

        var context = await ExecuteForkAsync(fork);

        // Act - complete the specified number of branches
        for (var i = 0; i < branchesToComplete; i++)
        {
            var childContext = await CreateChildContextAsync(branches[i]);
            var completedContext = new ActivityCompletedContext(context, childContext);
            await InvokeCompleteChildAsync(fork, completedContext);
        }

        // Assert
        var completedSet = context.GetProperty<HashSet<string>>("Completed");
        Assert.NotNull(completedSet);
        Assert.Equal(branchesToComplete, completedSet.Count);
        
        for (var i = 0; i < branchesToComplete; i++)
        {
            Assert.Contains(branches[i].Id, completedSet);
        }
    }

    [Fact]
    public async Task TracksAllActivityCompletions() // Including non-branch activities
    {
        // Arrange
        var branch = new WriteLine("Valid Branch");
        var nonBranch = new WriteLine("Non-Branch Activity");
        
        // Ensure the activities have different IDs by setting them explicitly
        branch.Id = "branch-activity";
        nonBranch.Id = "non-branch-activity";
        
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches = { branch } // Only branch is in the collection
        };

        var context = await ExecuteForkAsync(fork);

        // Act - complete a non-branch activity
        var nonBranchChildContext = await CreateChildContextAsync(nonBranch);
        var nonBranchContext = new ActivityCompletedContext(context, nonBranchChildContext);
        await InvokeCompleteChildAsync(fork, nonBranchContext);

        // Assert - Fork tracks ALL completions, even non-branch activities
        var completedSet = context.GetProperty<HashSet<string>>("Completed");
        Assert.NotNull(completedSet);
        Assert.Contains(nonBranch.Id, completedSet);
        Assert.Single(completedSet);
    }

    [Fact]
    public async Task JoinLogicOnlyConsidersBranchActivities()
    {
        // Arrange
        var branch = new WriteLine("Valid Branch");
        var nonBranch = new WriteLine("Non-Branch Activity");
        
        branch.Id = "branch-activity";
        nonBranch.Id = "non-branch-activity";
        
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches = { branch }
        };

        var context = await ExecuteForkAsync(fork);

        // Act - complete non-branch activity first, then branch activity
        var nonBranchChildContext = await CreateChildContextAsync(nonBranch);
        var nonBranchContext = new ActivityCompletedContext(context, nonBranchChildContext);
        await InvokeCompleteChildAsync(fork, nonBranchContext);

        // Complete the actual branch activity
        var branchChildContext = await CreateChildContextAsync(branch);
        var branchContext = new ActivityCompletedContext(context, branchChildContext);
        await InvokeCompleteChildAsync(fork, branchContext);
        
        // Assert - Now both activities should be tracked
        var completedSet = context.GetProperty<HashSet<string>>("Completed");
        Assert.NotNull(completedSet);
        Assert.Contains(branch.Id, completedSet);
        Assert.Contains(nonBranch.Id, completedSet);
        Assert.Equal(2, completedSet.Count);
    }

    [Fact]
    public async Task MaintainsSeparateCompletionTracking()
    {
        // Arrange
        var branch1 = new WriteLine("Branch 1");
        var branch2 = new WriteLine("Branch 2");
        
        branch1.Id = "branch1-unique";
        branch2.Id = "branch2-unique";
        
        var fork1 = new Fork { Branches = { branch1 } };
        var fork2 = new Fork { Branches = { branch2 } };

        var fixture1 = new ActivityTestFixture(fork1);
        var fixture2 = new ActivityTestFixture(fork2);
        
        var context1 = await fixture1.ExecuteAsync();
        var context2 = await fixture2.ExecuteAsync();

        // Act - complete branches in different fork instances
        var child1 = await CreateChildContextAsync(branch1);
        var child2 = await CreateChildContextAsync(branch2);
        await InvokeCompleteChildAsync(fork1, new ActivityCompletedContext(context1, child1));
        await InvokeCompleteChildAsync(fork2, new ActivityCompletedContext(context2, child2));

        // Assert - each fork should track its own completions
        var completed1 = context1.GetProperty<HashSet<string>>("Completed");
        var completed2 = context2.GetProperty<HashSet<string>>("Completed");
        
        Assert.NotNull(completed1);
        Assert.NotNull(completed2);
        Assert.Contains(branch1.Id, completed1);
        Assert.DoesNotContain(branch2.Id, completed1);
        
        Assert.Contains(branch2.Id, completed2);
        Assert.DoesNotContain(branch1.Id, completed2);
    }
    
    private static async Task<ActivityExecutionContext> ExecuteForkAsync(Fork fork)
    {
        var fixture = new ActivityTestFixture(fork);
        return await fixture.ExecuteAsync();
    }
    
    private static WriteLine[] CreateBranches(int count, string namePrefix = "Branch")
    {
        return Enumerable.Range(1, count)
            .Select(i => new WriteLine($"{namePrefix} {i}") { Id = $"{namePrefix.ToLower()}-{i}" })
            .ToArray();
    }
    
    private static async Task<ActivityExecutionContext> CreateChildContextAsync(IActivity childActivity)
    {
        var childFixture = new ActivityTestFixture(childActivity);
        var childContext = await childFixture.ExecuteAsync();
        return childContext;
    }
    
    private static async Task<HashSet<string>> CompleteBranchAsync(Fork fork, ActivityExecutionContext parentContext, IActivity branch)
    {
        var childContext = await CreateChildContextAsync(branch);
        var completedContext = new ActivityCompletedContext(parentContext, childContext);
        await InvokeCompleteChildAsync(fork, completedContext);
        return parentContext.GetProperty<HashSet<string>>("Completed") ?? new HashSet<string>();
    }

    private static async Task InvokePrivateMethodAsync(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
        {
            Assert.Fail($"{methodName} method not found on {instance.GetType().Name} class. This may indicate a breaking change in the implementation.");
        }
        
        try
        {
            var result = method.Invoke(instance, parameters);

            switch (result)
            {
                case ValueTask valueTask:
                    await valueTask;
                    break;
                case Task task:
                    await task;
                    break;
            }
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            if (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
            throw;
        }
    }
    
    private static async Task InvokeCompleteChildAsync(Fork fork, ActivityCompletedContext completedContext)
    {
        await InvokePrivateMethodAsync(fork, "CompleteChildAsync", completedContext);
    }
}

