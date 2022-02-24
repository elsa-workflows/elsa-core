using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Extensions;
using Elsa.Samples.MaskPii.Filters;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Register a workflow journal filter that masks user passwords.
services.AddSingleton<IWorkflowJournalFilter, RedactPasswordFilter>();

// Register the "data masking" module.
services.AddDataMasking();

// Register Elsa services.
services
    .AddElsa(elsa => elsa
        // Register HTTP activities.
        .AddHttpActivities(options => options.BasePath = "/workflows")

        // Register custom activities from this assembly.
        .AddActivitiesFrom<Program>()

        // Register workflows from this assembly.
        .AddWorkflowsFrom<Program>());

var app = builder.Build();

// Install HTTP Endpoint middleware that invokes our workflows that use HTTP Endpoint activities. 
app.UseHttpActivities();

// Display a default welcome page in case nothing matches the HTTP request.
app.UseWelcomePage();
app.Run();