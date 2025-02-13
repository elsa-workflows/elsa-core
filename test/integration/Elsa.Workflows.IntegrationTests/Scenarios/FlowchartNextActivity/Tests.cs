using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity.Workflows;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity;

public class FlowchartNextActivityTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;

    public FlowchartNextActivityTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<FlowchartNextActivityTests>()
            .Build();

        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Flowchart only schedules next activity connected to outcome of previous activity.")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<FlowchartWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1" }, lines);
    }

    [Fact(DisplayName = "Flowchart with backward connections and a dangling activity")]
    public async Task BackwardConnectionTest()
    {
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var loopVariable = new Variable<int>("LoopCount", 0);

            var start = new Start();
            var dangling = new WriteLine("dangling");
            var writeLineDecision = new FlowSwitch()
            {
                Cases = {
                    new FlowSwitchCase("LessThanThree", new Expression("JavaScript", "getVariable('LoopCount') < 3")),
                    new FlowSwitchCase("LessThanOne", new Expression("JavaScript", "getVariable('LoopCount') < 1")),
                }, 
                Mode = new(SwitchMode.MatchAny)
            };
            var a = new WriteLine("A");
            var b = new WriteLine("B");
            var c = new WriteLine("C");
            var incrementLoop = new SetVariable()
            {
                Variable = loopVariable,
                Value = new Models.Input<object?>(new Expression("JavaScript", "getVariable('LoopCount') + 1"))
            };
            var loopbackDecision = new FlowSwitch()
            {
                Cases = {
                    new FlowSwitchCase("EqualOne", new Expression("JavaScript", "getVariable('LoopCount') == 1")),
                    new FlowSwitchCase("LessThanFour", new Expression("JavaScript", "getVariable('LoopCount') < 4")),
                }, 
                Mode = new(SwitchMode.MatchFirst)
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
                        c,
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
                    new(new Endpoint(writeLineDecision, "LessThanThree"), new Endpoint(a)),
                    new(new Endpoint(writeLineDecision, "LessThanThree"), new Endpoint(b)),
                    new(new Endpoint(writeLineDecision, "LessThanOne"), new Endpoint(c)),
                    new(new Endpoint(writeLineDecision, "Default"), new Endpoint(incrementLoop)),
                    new(a, incrementLoop),
                    new(b, incrementLoop),
                    new(c, incrementLoop),
                    new(incrementLoop, loopbackDecision),
                    new(new Endpoint(loopbackDecision, "EqualOne"), new Endpoint(d)),
                    new(d, incrementLoop),
                    new(new Endpoint(loopbackDecision, "LessThanFour"), new Endpoint(e)),
                    new(e, writeLineDecision),
                    new(new Endpoint(loopbackDecision, "Default"), new Endpoint(f)),
                    new(f, end),
                }
            };
        });

        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Equal(new[] { "A", "B", "C", "D", "E", "A", "B", "E", "F" }, lines);
    }

    [Fact(DisplayName = "Flowchart with an invalid backward connection")]
    public async Task InvalidBackwardConnectionTest()
    {
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var start = new Start() { Id = "Start" };
            var a = new WriteLine("A") { Id = "WriteLineA" };
            var b = new WriteLine("B") { Id = "WriteLineB" };
            var c = new WriteLine("C") { Id = "WriteLineC" };
            var d = new WriteLine("D") { Id = "WriteLineD" };
            var e = new WriteLine("E") { Id = "WriteLineE" };

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

        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);
        Assert.Equal(1, result.WorkflowState.Incidents.Count());
        Assert.Equal("Invalid backward connection: Every path from the source ('WriteLineE') must go through the target ('WriteLineC') when tracing back to the start.", result.WorkflowState.Incidents.First().Message);
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, lines);
    }

    [Theory(DisplayName = "Flowchart with a Join activity executed multiple times")]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAny)]
    public async Task WaitAnyLoopTest(FlowJoinMode joinMode)
    {
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var loopVariable = new Variable<int>("LoopCount", 0);

            var start = new Start();
            var a = new WriteLine("A");
            var b = new WriteLine("B");
            var c = new WriteLine("C");
            var d = new WriteLine("D");
            var join = new FlowJoin()
            {
                Mode = new(joinMode)
            };
            var incrementLoop = new SetVariable()
            {
                Variable = loopVariable,
                Value = new Models.Input<object?>(new Expression("JavaScript", "getVariable('LoopCount') + 1"))
            };
            var loopbackDecision = new FlowSwitch()
            {
                Cases = {
                    new FlowSwitchCase("LessThanThree", new Expression("JavaScript", "getVariable('LoopCount') < 3")),
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
                    new(incrementLoop,loopbackDecision),
                    new(new Endpoint(loopbackDecision, "LessThanThree"), new Endpoint(a)),
                    new(new Endpoint(loopbackDecision, "Default"), new Endpoint(end)),
                }
            };
        });

        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Equal(new[] { "A", "B", "C", "D", "A", "B", "C", "D", "A", "B", "C", "D"}, lines);
    }
}