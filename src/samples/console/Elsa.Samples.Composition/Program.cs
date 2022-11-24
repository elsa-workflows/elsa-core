using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Declare a workflow variable for use in the workflow.
var personVariable = new Variable<Person>();

// Declare a workflow.
var workflow = new Sequence
{
    Variables = { personVariable },
    Activities =
    {
        new AskDetails
        {
            NamePrompt = new Input<string>("What's your name?"),
            AgePrompt = new Input<string>("What's your age?"),
            Result = new Output(personVariable)
        },
        new WriteLine(context =>
        {
            var person = personVariable.Get(context)!;
            return $"Your name is {person.Name} and you are {person.Age} years old.";
        })
    }
};

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync(workflow);