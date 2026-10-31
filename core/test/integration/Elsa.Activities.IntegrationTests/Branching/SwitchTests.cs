using System.Collections;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Branching;

/// <summary>
/// Integration tests for Switch activity.
/// </summary>
public class SwitchTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);
    
    [Fact(DisplayName = "Switch completes immediately when no matches and no activities scheduled")]
    public async Task Switch_CompletesImmediately_WhenNoMatchesAndNoActivitiesScheduled()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");
        var case2Activity = new WriteLine("Case 2");

        var switchActivity = new Switch
        {
            Cases =
            {
                new("Case 1", () => false, case1Activity),
                new("Case 2", () => false, case2Activity)
            },
            Default = null
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivityCompleted(switchActivity);
        result.AssertActivitiesNotExecuted(case1Activity, case2Activity);
    }

    [Fact(DisplayName = "Switch completes immediately when no matches but has null default")]
    public async Task Switch_CompletesImmediately_WhenNoMatchesAndDefaultIsNull()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");

        var switchActivity = new Switch
        {
            Cases =
            {
                new("Case 1", () => false, case1Activity)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivityCompleted(switchActivity);
        result.AssertActivityNotExecuted(case1Activity);
    }

    [Fact(DisplayName = "Switch schedules default activity when no cases match")]
    public async Task Switch_SchedulesDefaultActivity_WhenNoCasesMatch()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");
        var case2Activity = new WriteLine("Case 2");
        var defaultActivity = new WriteLine("Default");

        var switchActivity = new Switch
        {
            Cases =
            {
                new("Case 1", () => false, case1Activity),
                new("Case 2", () => false, case2Activity)
            },
            Default = defaultActivity
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, defaultActivity);
        result.AssertActivitiesNotExecuted(case1Activity, case2Activity);
    }

    [Fact(DisplayName = "Switch in MatchFirst mode schedules only first matching case")]
    public async Task Switch_InMatchFirstMode_SchedulesOnlyFirstMatchingCase()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");
        var case2Activity = new WriteLine("Case 2");
        var case3Activity = new WriteLine("Case 3");

        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases =
            {
                new("Case 1", () => false, case1Activity),
                new("Case 2", () => true, case2Activity),
                new("Case 3", () => true, case3Activity)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, case2Activity);
        result.AssertActivitiesNotExecuted(case1Activity, case3Activity);
    }

    [Fact(DisplayName = "Switch in MatchAny mode schedules all matching cases")]
    public async Task Switch_InMatchAnyMode_SchedulesAllMatchingCases()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");
        var case2Activity = new WriteLine("Case 2");
        var case3Activity = new WriteLine("Case 3");
        var case4Activity = new WriteLine("Case 4");
        var case5Activity = new WriteLine("Case 5");

        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases =
            {
                new("Case 1", () => false, case1Activity),
                new("Case 2", () => true, case2Activity),
                new("Case 3", () => true, case3Activity),
                new("Case 4", () => false, case4Activity),
                new("Case 5", () => true, case5Activity)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, case2Activity, case3Activity, case5Activity);
        result.AssertActivitiesNotExecuted(case1Activity, case4Activity);
    }

    [Fact(DisplayName = "Switch completes only after all scheduled activities complete")]
    public async Task Switch_CompletesOnlyAfterAllScheduledActivitiesComplete()
    {
        // Arrange
        var activity1 = new WriteLine("Activity 1");
        var activity2 = new WriteLine("Activity 2");
        var activity3 = new WriteLine("Activity 3");

        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases =
            {
                new("Case 1", () => true, activity1),
                new("Case 2", () => true, activity2),
                new("Case 3", () => true, activity3)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, activity1, activity2, activity3);
    }

    [Fact(DisplayName = "Switch completes after default activity completes")]
    public async Task Switch_CompletesAfterDefaultActivityCompletes()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");
        var defaultActivity = new WriteLine("Default Activity");

        var switchActivity = new Switch
        {
            Cases =
            {
                new("Case 1", () => false, case1Activity)
            },
            Default = defaultActivity
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, defaultActivity);
        result.AssertActivityNotExecuted(case1Activity);
    }

    [Fact(DisplayName = "Switch with nested activities completes after all complete")]
    public async Task Switch_WithNestedActivities_CompletesAfterAllComplete()
    {
        // Arrange
        var nestedSequence1 = new Sequence
        {
            Activities =
            {
                new WriteLine("Nested 1A"),
                new WriteLine("Nested 1B")
            }
        };

        var nestedSequence2 = new Sequence
        {
            Activities =
            {
                new WriteLine("Nested 2A"),
                new WriteLine("Nested 2B")
            }
        };

        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases =
            {
                new("Case 1", () => true, nestedSequence1),
                new("Case 2", () => true, nestedSequence2)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, nestedSequence1, nestedSequence2);
    }

    [Fact(DisplayName = "Switch in MatchFirst mode with single match")]
    public async Task Switch_InMatchFirstMode_WithSingleMatch()
    {
        // Arrange
        var case1Activity = new WriteLine("Case 1");
        var case2Activity = new WriteLine("Case 2");
        var case3Activity = new WriteLine("Case 3");

        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchFirst),
            Cases =
            {
                new("Case 1", () => false, case1Activity),
                new("Case 2", () => false, case2Activity),
                new("Case 3", () => true, case3Activity)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, case3Activity);
        result.AssertActivitiesNotExecuted(case1Activity, case2Activity);
    }

    [Fact(DisplayName = "Switch evaluates conditions with expression context")]
    public async Task Switch_EvaluatesConditionsWithExpressionContext()
    {
        // Arrange
        var conditionValue = true;
        var case1Activity = new WriteLine("Case 1");
        var case2Activity = new WriteLine("Case 2");
        var case3Activity = new WriteLine("Case 3");

        var switchActivity = new Switch
        {
            Cases =
            {
                new("Case 1", _ => false, case1Activity),
                new("Case 2", _ => conditionValue, case2Activity),
                new("Case 3", _ => false, case3Activity)
            }
        };

        // Act
        var result = await _fixture.RunActivityAsync(switchActivity);

        // Assert
        result.AssertActivitiesCompleted(switchActivity, case2Activity);
        result.AssertActivitiesNotExecuted(case1Activity, case3Activity);
    }

    [Fact(DisplayName = "Switch completes after a branch suspends and resumes from a restored string list")]
    public async Task Switch_Completes_AfterBranchSuspendsAndResumesFromPersistedState()
    {
        // Arrange
        var wait1 = new BlockingActivity { Id = "wait-1" };
        var wait2 = new BlockingActivity { Id = "wait-2" };
        var (switchActivity, workflow, runner) = await CreateMatchAnySwitchAsync(wait1, wait2);

        // Act
        var state = RoundTrip(await RunUntilSuspendedAsync(runner, workflow));
        RestoreScheduledActivityIds(state, ids => ids.ToList());
        state = RoundTrip((await ResumeAsync(runner, workflow, state, wait1.Id)).WorkflowState);
        var resumed = await ResumeAsync(runner, workflow, state, wait2.Id);

        // Assert
        resumed.AssertActivityCompleted(switchActivity);
        resumed.AssertWorkflowCompleted();
    }

    [Fact(DisplayName = "Switch in MatchAny mode completes after each branch resumes from a save-and-reload")]
    public async Task Switch_Completes_AfterAllMatchingBranchesResumeFromPersistedState()
    {
        // Arrange
        var wait1 = new BlockingActivity { Id = "wait-1" };
        var wait2 = new BlockingActivity { Id = "wait-2" };
        var (switchActivity, workflow, runner) = await CreateMatchAnySwitchAsync(wait1, wait2);

        // Act
        var state = RoundTrip(await RunUntilSuspendedAsync(runner, workflow));
        var afterFirst = await ResumeAsync(runner, workflow, state, wait1.Id);

        Assert.Equal(WorkflowSubStatus.Suspended, afterFirst.WorkflowState.SubStatus);
        Assert.Single(afterFirst.WorkflowState.Bookmarks);

        var resumed = await ResumeAsync(runner, workflow, RoundTrip(afterFirst.WorkflowState), wait2.Id);

        // Assert
        resumed.AssertActivityCompleted(switchActivity);
        resumed.AssertWorkflowCompleted();
    }

    [Fact(DisplayName = "Switch completes when scheduled IDs are restored as a legacy object list")]
    public async Task Switch_Completes_WhenScheduledActivityIdsAreRestoredAsObjectList()
    {
        // Arrange
        var wait1 = new BlockingActivity { Id = "wait-1" };
        var wait2 = new BlockingActivity { Id = "wait-2" };
        var (switchActivity, workflow, runner) = await CreateMatchAnySwitchAsync(wait1, wait2);

        // Act
        var state = RoundTrip(await RunUntilSuspendedAsync(runner, workflow));
        RestoreScheduledActivityIds(state, ids => ids.Cast<object>().ToList());
        state = RoundTrip((await ResumeAsync(runner, workflow, state, wait1.Id)).WorkflowState);
        var resumed = await ResumeAsync(runner, workflow, state, wait2.Id);

        // Assert
        resumed.AssertActivityCompleted(switchActivity);
        resumed.AssertWorkflowCompleted();
    }

    private async Task<(Switch Switch, Workflow Workflow, IWorkflowRunner Runner)> CreateMatchAnySwitchAsync(
        BlockingActivity wait1,
        BlockingActivity wait2)
    {
        var switchActivity = new Switch
        {
            Mode = new(SwitchMode.MatchAny),
            Cases =
            {
                new("One", () => true, wait1),
                new("Two", () => true, wait2)
            }
        };

        await _fixture.BuildAsync();
        return (switchActivity, Workflow.FromActivity(switchActivity), _fixture.Services.GetRequiredService<IWorkflowRunner>());
    }

    private static async Task<WorkflowState> RunUntilSuspendedAsync(IWorkflowRunner runner, Workflow workflow)
    {
        var started = await runner.RunAsync(workflow);
        Assert.Equal(WorkflowSubStatus.Suspended, started.WorkflowState.SubStatus);
        Assert.Equal(2, started.WorkflowState.Bookmarks.Count);
        return started.WorkflowState;
    }

    private WorkflowState RoundTrip(WorkflowState state)
    {
        var serializer = _fixture.Services.GetRequiredService<IWorkflowStateSerializer>();
        return serializer.Deserialize(serializer.Serialize(state));
    }

    private static async Task<RunWorkflowResult> ResumeAsync(IWorkflowRunner runner, Workflow workflow, WorkflowState state, string activityId)
    {
        var bookmark = Assert.Single(state.Bookmarks, x => x.ActivityId == activityId);
        return await runner.RunAsync(workflow, state, new RunWorkflowOptions
        {
            BookmarkId = bookmark.Id
        });
    }

    private static void RestoreScheduledActivityIds(WorkflowState state, Func<IEnumerable<string>, object> restore)
    {
        var properties = Assert.Single(state.ActivityExecutionContexts, x => x.Properties.ContainsKey("ScheduledActivityIds")).Properties;
        var ids = ((IEnumerable)properties["ScheduledActivityIds"]).Cast<object>().Select(x => Convert.ToString(x)!);
        properties["ScheduledActivityIds"] = restore(ids);
    }

    /// <summary>
    /// Suspends by creating a bookmark so the Switch branch can be persisted and resumed.
    /// </summary>
    private sealed class BlockingActivity : Activity
    {
        protected override void Execute(ActivityExecutionContext context) => context.CreateBookmark();
    }
}
