using Elsa;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.LoadAndRunFromDatabaseConsole;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

// Create a service container with Elsa services.
var services = new ServiceCollection()
    .AddElsa(options => options
        .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
        .AddConsoleActivities()
    )
    .BuildServiceProvider();

// Run DB migrations.
await services.GetRequiredService<IStartupRunner>().StartupAsync();

// Get a workflow definition store for saving & loading workflow definitions.
var workflowDefinitionStore = services.GetRequiredService<IWorkflowDefinitionStore>();

// Create a workflow to store in DB (for demo purposes).
var workflowDefinition = WorkflowDefinitionBuilder.CreateDemoWorkflowDefinition();

// Add or update the workflow definition.
await workflowDefinitionStore.SaveAsync(workflowDefinition);

// Load the workflow definition from DB.
workflowDefinition = (await workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinition.DefinitionId, VersionOptions.Published))!;

// To execute a workflow definition, we need to create a workflow blueprint from it.
var workflowBlueprintMaterializer = services.GetRequiredService<IWorkflowBlueprintMaterializer>();
var workflowBlueprint = await workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition);

// Get a workflow runner.
var workflowRunner = services.GetRequiredService<IStartsWorkflow>();

// Run the workflow.
await workflowRunner.StartWorkflowAsync(workflowBlueprint);