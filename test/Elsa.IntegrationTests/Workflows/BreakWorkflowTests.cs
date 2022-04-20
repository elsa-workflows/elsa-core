using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Builders;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Workflows;

public class BreakWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public BreakWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Break exits out of ForEach")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakForEachWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "C#", "Test", "End" }, lines);
    }

    [Fact(DisplayName = "Break exits out of immediate ForEach only")]
    public async Task Test2()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new NestedForEachWithBreakWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "C#", "Classes", "Rust", "Classes", "Go", "Classes" }, lines);
    }

    [Fact(DisplayName = "Break exits out of For")]
    public async Task Test3()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakForWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "0", "1", "End" }, lines);
    }
    
    [Fact(DisplayName = "Break exits out of While")]
    public async Task Test4()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakWhileWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "1", "2", "End" }, lines);
    }

    private class BreakForEachWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var items = new[] { "C#", "Rust", "Go" };
            var currentItem = new Variable<string>();

            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new WriteLine("Start"),
                    new ForEach<string>
                    {
                        Items = new Input<ICollection<string>>(items),
                        CurrentValue = currentItem,
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new If(context => currentItem.Get(context) == "Rust")
                                {
                                    Then = new Break()
                                },
                                new WriteLine(currentItem),
                                new WriteLine("Test")
                            }
                        }
                    },
                    new WriteLine("End"),
                }
            });
        }
    }

    private class NestedForEachWithBreakWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var outerItems = new[] { "C#", "Rust", "Go" };
            var innerItems = new[] { "Classes", "Functions", "Modules" };
            var currentOuterItem = new Variable<string>();
            var currentInnerItem = new Variable<string>();

            workflow.WithRoot(new ForEach<string>(outerItems)
            {
                CurrentValue = currentOuterItem,
                Body = new Sequence
                {
                    Activities =
                    {
                        new WriteLine(currentOuterItem),
                        new ForEach<string>(innerItems)
                        {
                            CurrentValue = currentInnerItem,
                            Body = new Sequence
                            {
                                Activities =
                                {
                                    new If(context => currentInnerItem.Get(context) == "Functions")
                                    {
                                        Then = new Break()
                                    },
                                    new WriteLine(currentInnerItem)
                                }
                            }
                        }
                    }
                }
            });
        }
    }

    private class BreakForWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var currentValue = new Variable<int?>();

            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new WriteLine("Start"),
                    new For(0, 3)
                    {
                        CurrentValue = currentValue,
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new If(context => currentValue.Get(context) == 2)
                                {
                                    Then = new Break()
                                },
                                new WriteLine(context => currentValue.Get(context).ToString()),
                            }
                        }
                    },
                    new WriteLine("End"),
                }
            });
        }
    }

    private class BreakWhileWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var currentValue = new Variable<int?>(0);

            workflow.WithRoot(new Sequence
            {
                Variables = { currentValue },
                Activities =
                {
                    new WriteLine("Start"),
                    While.True(new Sequence
                    {
                        Activities =
                        {
                            new SetVariable
                            {
                                Variable = currentValue,
                                Value = new Input<object?>(context => currentValue.Get(context) + 1)
                            },
                            new If(context => currentValue.Get(context) == 3)
                            {
                                Then = new Break()
                            },
                            new WriteLine(context => currentValue.Get(context).ToString()),
                        }
                    }),
                    new WriteLine("End"),
                }
            });
        }
    }
}