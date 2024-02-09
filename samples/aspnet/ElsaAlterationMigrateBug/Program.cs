// See https://aka.ms/new-console-template for more information

using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Extensions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using FluentStorage;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = CreateServiceProvider();
var registriesPopulator = serviceProvider.GetRequiredService<IRegistriesPopulator>();
await registriesPopulator.PopulateAsync();

var workflowDefinitionStore = serviceProvider.GetRequiredService<IWorkflowDefinitionStore>();
var workflowRuntime = serviceProvider.GetRequiredService<IWorkflowRuntime>();
var alterationRunner = serviceProvider.GetRequiredService<IAlterationRunner>();
var workflow = await FindWorkflowDefinition(1);
var startOptions = new StartWorkflowRuntimeOptions
{
    CorrelationId = "1",
    VersionOptions = VersionOptions.SpecificVersion(workflow.Version)
};

var item = await workflowRuntime.StartWorkflowAsync(workflow.DefinitionId, startOptions);

await MigrateWorkflow(item.WorkflowInstanceId);
Console.WriteLine("Executed workflow");
Console.ReadLine();
return;

async Task<WorkflowDefinition?> FindWorkflowDefinition(int version)
{
    var filter = new WorkflowDefinitionFilter()
    {
        Name = "AlterationTest",
        VersionOptions = VersionOptions.SpecificVersion(version)
    };
    var workflowBlueprint = await workflowDefinitionStore.FindAsync(filter);
    return workflowBlueprint;
}

async Task MigrateWorkflow(string workflowInstanceId)
{
    var alterations = new List<IAlteration>
    {
        new Migrate
        {
            TargetVersion = 2
        }
    };

    await alterationRunner.RunAsync([workflowInstanceId], alterations);
}

ServiceProvider CreateServiceProvider()
{
    var services = new ServiceCollection();
    services.AddElsa(elsa =>
    {
        var currentAssemblyPath = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        var workflowsDirectory = Path.Combine(currentAssemblyPath, "Workflows");

        elsa.UseFluentStorageProvider(storage => storage.BlobStorage = _ => StorageFactory.Blobs.DirectoryFiles(Path.Combine(workflowsDirectory)))
            .UseFileStorage()
            .UseAlterations();
    });

    var serviceProvider1 = services.BuildServiceProvider();
    return serviceProvider1;
}