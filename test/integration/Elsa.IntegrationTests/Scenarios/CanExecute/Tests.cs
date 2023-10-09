using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.IntegrationTests.Scenarios.CanExecute.Activities;
using Elsa.IntegrationTests.Scenarios.CanExecute.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.CanExecute;

public class CanExecuteTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;

    public CanExecuteTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<CustomActivity>()
            .Build();

        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Theory(DisplayName = "Activities are executed only when they report that they can execute.")]
    [InlineData(12, "Magic number is 12")]
    [InlineData(42, "Magic number is 42\nWelcome to the world of Might and Magic!\nDone")]
    public async Task Test1(int magicNumber, string expectedLines)
    {
        await _services.PopulateRegistriesAsync();
        var runOptions = new RunWorkflowOptions
        {
            Input = new Dictionary<string, object>
            {
                ["MagicNumber"] = magicNumber
            }
        };
        await _workflowRunner.RunAsync<MagicWorkflow>(runOptions);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(expectedLines.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), lines);
    }
}