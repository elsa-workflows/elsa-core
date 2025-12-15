using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity;

public class FlowchartNextActivityTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper).AddActivitiesFrom<FlowchartNextActivityTests>();

    [Fact(DisplayName = "Flowchart only schedules next activity connected to outcome of previous activity.")]
    public async Task FlowchartOnlySchedulesNextConnectedActivity()
    {
        await _fixture.RunWorkflowAsync<FlowchartWorkflow>();
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(["Line 1"], lines);
    }

    [Fact(DisplayName = "Flowchart with backward connections and a dangling activity")]
    public async Task BackwardConnectionTest()
    {
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var loopVariable = new Variable<int>("LoopCount", 0);

            var start = new Start();
            var dangling = new WriteLine("dangling");
            var writeLineDecision = new FlowSwitch
            {
                Cases =
                {
                    new("LessThanThree", new Expression("JavaScript", "getVariable('LoopCount') < 3"))
                },
                Mode = new(SwitchMode.MatchAny)
            };
            var a = new WriteLine("A");
            var b = new WriteLine("B");
            var incrementLoop = new SetVariable
            {
                Variable = loopVariable,
                Value = new(new Expression("JavaScript", "getVariable('LoopCount') + 1"))
            };
            var loopbackDecision = new FlowSwitch
            {
                Cases =
                {
                    new("EqualOne", new Expression("JavaScript", "getVariable('LoopCount') == 1")),
                    new("LessThanFour", new Expression("JavaScript", "getVariable('LoopCount') < 4")),
                    new("EqualThree", new Expression("JavaScript", "getVariable('LoopCount') == 3")),
                },
                Mode = new(SwitchMode.MatchAny)
            };
            var d = new WriteLine("D");
            var e = new WriteLine("E");
            var f = new WriteLine("F");
            var end = new End();

            workflowBuilder.Root = new Flowchart
            {
                Variables =
                {
                    loopVariable
                },
                Activities =
                {
                    start,
                    dangling,
                    writeLineDecision,
                    a,
                    b,
                    incrementLoop,
                    loopbackDecision,
                    d,
                    e,
                    f,
                    end
                },
                Connections =
                {
                    new(start, writeLineDecision),
                    new(dangling, writeLineDecision),
                    new(new(writeLineDecision, "LessThanThree"), new Endpoint(a)),
                    new(new(writeLineDecision, "LessThanThree"), new Endpoint(b)),
                    new(new(writeLineDecision, "Default"), new Endpoint(incrementLoop)),
                    new(a, incrementLoop),
                    new(b, incrementLoop),
                    new(incrementLoop, loopbackDecision),
                    new(new(loopbackDecision, "EqualOne"), new Endpoint(d)),
                    new(d, incrementLoop),
                    new(new(loopbackDecision, "LessThanFour"), new Endpoint(e)),
                    new(e, writeLineDecision),
                    new(new(loopbackDecision, "EqualThree"), new Endpoint(f)),
                    new(f, end),
                }
            };
        });

        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Equal(new[]
        {
            "A", "B", "D", "E", "A", "B", "E", "E", "F"
        }, lines);
    }

    [Fact(DisplayName = "Flowchart with an invalid backward connection (counter-based mode only)")]
    public async Task InvalidBackwardConnectionTest()
    {
        // This test is only valid for counter-based mode
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var start = new Start
            {
                Id = "Start"
            };
            var a = new WriteLine("A")
            {
                Id = "WriteLineA"
            };
            var b = new WriteLine("B")
            {
                Id = "WriteLineB"
            };
            var c = new WriteLine("C")
            {
                Id = "WriteLineC"
            };
            var d = new WriteLine("D")
            {
                Id = "WriteLineD"
            };
            var e = new WriteLine("E")
            {
                Id = "WriteLineE"
            };

            workflowBuilder.Root = new Flowchart
            {
                Activities =
                {
                    start,
                    a,
                    b,
                    c,
                    d,
                    e,
                },
                Connections =
                {
                    new(start, a),
                    new(a, b),
                    new(b, c),
                    new(b, d),
                    new(c, e),
                    new(d, e),
                    new(e, c),
                }
            };
        });

        var options = new RunWorkflowOptions().WithCounterBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync(workflow, options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);
        Assert.Single(result.WorkflowState.Incidents);
        Assert.Equal("Invalid backward connection: Every path from the source ('WriteLineE') must go through the target ('WriteLineC') when tracing back to the start.", result.WorkflowState.Incidents.First().Message);
        Assert.Equal(new[]
        {
            "A", "B", "C", "D", "E"
        }, lines);
    }

    [Theory(DisplayName = "Flowchart with a Join activity executed multiple times")]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    [InlineData(FlowJoinMode.WaitAny)]
    public async Task JoinLoopTest(FlowJoinMode joinMode)
    {
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var loopVariable = new Variable<int>("LoopCount", 0);

            var start = new Start();
            var a = new WriteLine("A");
            var b = new WriteLine("B");
            var c = new WriteLine("C");
            var d = new WriteLine("D");
            var join = new FlowJoin
            {
                Mode = new(joinMode)
            };
            var incrementLoop = new SetVariable
            {
                Variable = loopVariable,
                Value = new(new Expression("JavaScript", "getVariable('LoopCount') + 1"))
            };
            var loopbackDecision = new FlowSwitch
            {
                Cases =
                {
                    new("LessThanThree", new Expression("JavaScript", "getVariable('LoopCount') < 3")),
                },
                Mode = new(SwitchMode.MatchFirst)
            };
            var end = new End();

            workflowBuilder.Root = new Flowchart
            {
                Variables =
                {
                    loopVariable
                },
                Activities =
                {
                    start,
                    a,
                    b,
                    c,
                    d,
                    join,
                    incrementLoop,
                    loopbackDecision,
                    end
                },
                Connections =
                {
                    new(start, a),
                    new(a, b),
                    new(a, c),
                    new(a, d),
                    new(b, join),
                    new(c, join),
                    new(d, join),
                    new(join, incrementLoop),
                    new(incrementLoop, loopbackDecision),
                    new(new(loopbackDecision, "LessThanThree"), new Endpoint(a)),
                    new(new(loopbackDecision, "Default"), new Endpoint(end)),
                }
            };
        });

        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Equal(new[]
        {
            "A", "B", "C", "D", "A", "B", "C", "D", "A", "B", "C", "D"
        }, lines);
    }

    [Theory(DisplayName = "Flowchart with a Join activity executed multiple times, bug 6479")]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    [InlineData(FlowJoinMode.WaitAny)]
    public async Task JoinLoopBug6479Test(FlowJoinMode joinMode)
    {
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var loopVariable = new Variable<int>("LoopCount", 0);

            var start = new Start();
            var loopbackSwitch = new FlowSwitch
            {
                Cases =
                {
                    new("DoLoopback", new Expression("JavaScript", "getVariable('LoopCount') < 3")),
                },
                Mode = new(SwitchMode.MatchFirst)
            };
            var a = new WriteLine("A");
            var incrementLoop = new SetVariable
            {
                Variable = loopVariable,
                Value = new(new Expression("JavaScript", "getVariable('LoopCount') + 1"))
            };
            var join = new FlowJoin
            {
                Mode = new(joinMode)
            };
            var b = new WriteLine("B");
            var end = new End();

            workflowBuilder.Root = new Flowchart
            {
                Variables =
                {
                    loopVariable
                },
                Activities =
                {
                    start,
                    loopbackSwitch,
                    a,
                    incrementLoop,
                    join,
                    b,
                    end
                },
                Connections =
                {
                    new(start, loopbackSwitch),
                    new(new(loopbackSwitch, "DoLoopback"), new Endpoint(a)),
                    new(new(loopbackSwitch, "DoLoopback"), new Endpoint(incrementLoop)),
                    new(new(loopbackSwitch, "Default"), new Endpoint(b)),
                    new(a, join),
                    new(incrementLoop, join),
                    new(join, loopbackSwitch),
                    new(b, end),
                }
            };
        });

        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Equal(new[]
        {
            "A", "A", "A", "B"
        }, lines);
    }

    [Theory(DisplayName = "Flowchart Join behaves correctly (counter-based mode)")]
    [InlineData(false, FlowJoinMode.WaitAll, new[]
    {
        "A", "B", "C", "D", "F"
    })] // "E" is not scheduled because join has an unfollowed inbound connection
    [InlineData(false, FlowJoinMode.WaitAllActive, new[]
    {
        "A", "B", "C", "D", "E", "F"
    })] // "E" gets scheduled by join with an unfollowed inbound connection
    [InlineData(false, FlowJoinMode.WaitAny, new[]
    {
        "A", "B", "C", "D", "E", "F"
    })] // "E" only scheduled once
    [InlineData(true, FlowJoinMode.WaitAll, new[]
    {
        "A", "B", "C", "E", "F"
    })] // all Join inbound connections followed, "E" gets scheduled
    [InlineData(true, FlowJoinMode.WaitAllActive, new[]
    {
        "A", "B", "C", "E", "F"
    })] // all Join inbound connections followed, "E" gets scheduled
    [InlineData(true, FlowJoinMode.WaitAny, new[]
    {
        "A", "B", "C", "E", "F"
    })] // "E" only scheduled once
    //           Start
    //          /  |  \
    //         /   |   \
    //        A    B    C
    //       /     |    |
    //      |      |    |
    //   Decision  |    |
    //     /  \    |    |
    //    | (true) |   /
    //    |     \  |  /
    // (false)   \ | /
    //    |      Join
    //    D        |
    //     \       E
    //      \     /
    //       \   /
    //        \ /
    //         F
    public async Task JoinBehavesCorrectly(bool decisionResult, FlowJoinMode joinMode, string[] expectedLines)
    {
        // This test validates counter-based mode behavior
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var start = new Start()
            {
                Id = "Start"
            };
            var a = new WriteLine("A")
            {
                Id = "WriteLineA"
            };
            var b = new WriteLine("B")
            {
                Id = "WriteLineB"
            };
            var c = new WriteLine("C")
            {
                Id = "WriteLineC"
            };
            var decision = new FlowDecision()
            {
                Condition = new(new Literal<bool>(decisionResult))
            };
            var d = new WriteLine("D")
            {
                Id = "WriteLineD"
            };
            var join = new FlowJoin()
            {
                Mode = new(joinMode)
            };
            var e = new WriteLine("E")
            {
                Id = "WriteLineE"
            };
            var f = new WriteLine("F")
            {
                Id = "WriteLineF"
            };

            workflowBuilder.Root = new Flowchart
            {
                Activities =
                {
                    start,
                    a,
                    b,
                    c,
                    decision,
                    d,
                    join,
                    e,
                    f,
                },
                Connections =
                {
                    new(start, a),
                    new(start, b),
                    new(start, c),
                    new(a, decision),
                    new(new(decision, "True"), new Endpoint(join)),
                    new(new(decision, "False"), new Endpoint(d)),
                    new(b, join),
                    new(c, join),
                    new(d, f),
                    new(join, e),
                    new(e, f),
                }
            };
        });

        var options = new RunWorkflowOptions().WithCounterBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync(workflow, options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Equal(expectedLines, lines);
    }
}