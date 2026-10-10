using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using var standardOutput = new StringWriter();
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(new ConfigurationManager());
services.AddLogging();
services.AddElsa(elsa => elsa.UseWorkflows(workflows =>
    workflows.WithStandardOutStreamProvider(_ => new StandardOutStreamProvider(standardOutput))));

using var serviceProvider = services.BuildServiceProvider();
await serviceProvider.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();
var result = await workflowRunner.RunAsync<VariableScopingWorkflow>();

var outputLines = new List<string>();
using (var reader = new StringReader(standardOutput.ToString()))
{
    while (reader.ReadLine() is { } line)
    {
        outputLines.Add(line);
    }
}

if (!outputLines.SequenceEqual(new[] { "Sequence Value" }))
{
    throw new InvalidOperationException($"Expected exactly one standard-output line, 'Sequence Value'; received {JsonSerializer.Serialize(outputLines)}.");
}

if (result.WorkflowState.Status != WorkflowStatus.Finished || result.WorkflowState.SubStatus != WorkflowSubStatus.Finished)
{
    throw new InvalidOperationException($"Expected a finished workflow; received status {result.WorkflowState.Status} and sub-status {result.WorkflowState.SubStatus}.");
}

if (result.WorkflowState.Incidents.Count != 0)
{
    throw new InvalidOperationException($"Expected no workflow incidents; received {result.WorkflowState.Incidents.Count}.");
}

var loadedAssemblies = SelectedAssemblyProof.Snapshot();
Console.WriteLine("SELECTED_CONSUMER_PROOF=" + JsonSerializer.Serialize(new
{
    variableNearestScope = true,
    outputLines,
    status = result.WorkflowState.Status.ToString(),
    subStatus = result.WorkflowState.SubStatus.ToString(),
    incidents = result.WorkflowState.Incidents.Count,
    loadedAssemblies
}));

internal sealed class VariableScopingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var workflowLevelVariable = new Variable<string>("Foo", "Workflow Value");
        var sequenceLevelVariable = new Variable<string>("Foo", "Initial Value");

        workflow.Root = new Sequence
        {
            Variables = { workflowLevelVariable },
            Activities =
            {
                new Sequence
                {
                    Variables = { sequenceLevelVariable },
                    Activities =
                    {
                        new SetVariable
                        {
                            Variable = sequenceLevelVariable,
                            Value = new("Sequence Value")
                        },
                        new WriteLine(context => context.GetVariable<string>("Foo"))
                    }
                }
            }
        };
    }
}
