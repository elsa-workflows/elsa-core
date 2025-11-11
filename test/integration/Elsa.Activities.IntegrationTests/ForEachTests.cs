using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class ForEachTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _serviceProvider;
    private const string CurrentValueVar = "CurrentValue";

    public ForEachTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "ForEach executes each activity for every item in the collection")]
    public async Task ForEach_ExecutesEachActivity_ForEveryItem()
    {
        var expectedLines = new[]
        {
            "a", "b", "c"
        };
        var forEach = new ForEach<string>(expectedLines)
        {
            Body = WriteCurrentValue()
        };
        await RunAndAssertLines(forEach, expectedLines);
    }

    [Fact(DisplayName = "ForEach executes each activity for every item in the collection even if there's only one item")]
    public async Task ForEach_ExecutesEachActivity_ForSingleItem()
    {
        var expectedLines = new[]
        {
            "a"
        };
        var forEach = new ForEach<string>(expectedLines)
        {
            Body = WriteCurrentValue()
        };
        await RunAndAssertLines(forEach, expectedLines);
    }

    [Fact(DisplayName = "ForEach completes when the collection is empty")]
    public async Task ForEach_Completes_WhenCollectionIsEmpty()
    {
        string[] expectedLines = [];
        var forEach = new ForEach<string>(expectedLines)
        {
            Body = WriteCurrentValue()
        };
        var result = await _serviceProvider.RunActivityAsync(forEach);
        var journal = result.Journal;
        var forEachContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is ForEach<string>);
        Assert.NotNull(forEachContext);
        Assert.Equal(ActivityStatus.Completed, forEachContext.Status);
    }

    [Fact(DisplayName = "ForEach faults when the collection is null")]
    public async Task ForEach_Faults_WhenCollectionIsNull()
    {
        var forEach = new ForEach<string>((ICollection<string>)null!)
        {
            Body = WriteCurrentValue()
        };
        var result = await _serviceProvider.RunActivityAsync(forEach);
        var journal = result.Journal;
        var forEachContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is ForEach<string>);
        Assert.NotNull(forEachContext);
        Assert.Equal(ActivityStatus.Faulted, forEachContext.Status);
    }

    [Fact(DisplayName = "ForEach breaks when the Break activity executed")]
    public async Task ForEach_BreaksOutOfLoop_WhenExecutingBreakActivity()
    {
        var dataSource = new[]
        {
            "a", "b", "c"
        };
        var expectedLines = new[]
        {
            "a"
        };

        var forEach = new ForEach<string>(dataSource)
        {
            Body = new If
            {
                Condition = new(context => context.GetVariable<string>(CurrentValueVar) == "b"),
                Then = new Break(),
                Else = WriteCurrentValue()
            }
        };
        await RunAndAssertLines(forEach, expectedLines);
    }

    [Fact(DisplayName = "ForEach executes each activity for different item types")]
    public async Task ForEach_ExecutesEachActivity_ForDifferentItemTypes()
    {
        var dataSource = new object?[]
        {
            "a", 2, null, new Foo()
        };
        var expectedLines = new object[]
        {
            "a", "2", "", "Baz"
        };
        var forEach = new ForEach<object?>(dataSource)
        {
            Body = new WriteLine(context => context.GetVariable<object>(CurrentValueVar)?.ToString() ?? "")
        };
        await RunAndAssertLines(forEach, expectedLines);
    }

    [Fact(DisplayName = "ForEach completes when the end of the collection is reached even when an item is added to the collection at runtime")]
    public async Task ForEach_Completes_WhenItemAddedToCollectionAtRuntime()
    {
        var dataSource = new[]
        {
            "a", "b", "c"
        }.ToList();
        var expectedLines = new[]
        {
            "a", "b", "c", "e"
        };
        var dataSourceInput = new Input<ICollection<string>>(() => dataSource.ToList());
        var forEach = new ForEach<string>(dataSourceInput)
        {
            Body = new Sequence
            {
                Activities =
                [
                    new If(context => context.GetVariable<string>(CurrentValueVar) == "b")
                    {
                        Then = new Inline(inlineContext =>
                        {
                            dataSource.Add("e");
                            inlineContext.Set(dataSourceInput.MemoryBlockReference(), dataSource.ToList());
                        })
                    },
                    WriteCurrentValue()
                ]
            }
        };
        await RunAndAssertLines(forEach, expectedLines);
    }
    
    [Fact(DisplayName = "ForEach completes when the end of the collection is reached even when an item is removed from the collection at runtime")]
    public async Task ForEach_Completes_WhenItemRemovedFromCollectionAtRuntime()
    {
        var dataSource = new[]
        {
            "a", "b", "c"
        }.ToList();
        var expectedLines = new[]
        {
            "a", "b"
        };
        var dataSourceInput = new Input<ICollection<string>>(() => dataSource.ToList());
        var forEach = new ForEach<string>(dataSourceInput)
        {
            Body = new Sequence
            {
                Activities =
                [
                    new If(context => context.GetVariable<string>(CurrentValueVar) == "a")
                    {
                        Then = new Inline(inlineContext =>
                        {
                            dataSource.Remove("c");
                            inlineContext.Set(dataSourceInput.MemoryBlockReference(), dataSource.ToList());
                        })
                    },
                    WriteCurrentValue()
                ]
            }
        };
        await RunAndAssertLines(forEach, expectedLines);
    }
    
    [Fact(DisplayName = "ForEach completes when the end of the collection is reached even when an item is changed at runtime")]
    public async Task ForEach_Completes_WhenItemChangedAtRuntime()
    {
        var dataSource = new[]
        {
            "a", "b", "c"
        }.ToList();
        var expectedLines = new[]
        {
            "a", "b", "d"
        };
        var dataSourceInput = new Input<ICollection<string>>(() => dataSource.ToList());
        var forEach = new ForEach<string>(dataSourceInput)
        {
            Body = new Sequence
            {
                Activities =
                [
                    new If(context => context.GetVariable<string>(CurrentValueVar) == "b")
                    {
                        Then = new Inline(inlineContext =>
                        {
                            dataSource.Remove("c");
                            dataSource.Add("d");
                            inlineContext.Set(dataSourceInput.MemoryBlockReference(), dataSource.ToList());
                        })
                    },
                    WriteCurrentValue()
                ]
            }
        };
        await RunAndAssertLines(forEach, expectedLines);
    }
    
    [Fact(DisplayName = "ForEach remains in the Running state when an activity faults")]
    public async Task ForEach_Suspends_WhenActivityFaults()
    {
        var dataSource = new[]
        {
            "a", "b", "c"
        }.ToList();
        var expectedLines = new[]
        {
            "a", "b"
        };
        var forEach = new ForEach<string>(dataSource)
        {
            Body = new Sequence
            {
                Activities =
                [
                    new If(context => context.GetVariable<string>(CurrentValueVar) == "c")
                    {
                        Then = new Fault
                        {
                            Message = new("Faulted"),
                        }
                    },
                    WriteCurrentValue()
                ]
            }
        };
        var result = await _serviceProvider.RunActivityAsync(forEach);
        var journal = result.Journal;
        var forEachContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is ForEach<string>);
        Assert.Equal(expectedLines, _capturingTextWriter.Lines);
        Assert.NotNull(forEachContext);
        Assert.Equal(ActivityStatus.Running, forEachContext.Status);
        Assert.Equal(1, forEachContext.AggregateFaultCount);
    }
    
    private static WriteLine WriteCurrentValue() => new(context => context.GetVariable<string>(CurrentValueVar));
    
    private async Task RunAndAssertLines(IActivity activity, System.Collections.IEnumerable expected)
    {
        await _serviceProvider.RunActivityAsync(activity);
        Assert.Equal(expected, _capturingTextWriter.Lines);
    }
}

record Foo(string Bar = "Baz")
{
    public override string ToString() => Bar;
}