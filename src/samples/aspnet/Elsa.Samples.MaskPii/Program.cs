using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.MaskPii.Filters;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Register the "data masking" module.
services.AddDataMasking();

// Register a workflow journal filter that masks user passwords.
services.AddSingleton<IWorkflowJournalFilter, RedactPasswordFromHttpRequestFilter>();

// Register an activity state filter that masks user passwords.
services.AddSingleton<IActivityStateFilter, RedactPasswordFromStoreUserActivityFilter>();

// Add API endpoints for workflow designer.
services.AddElsaApiEndpoints();

// Configure CORS.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register Elsa services.
services
    .AddElsa(elsa => elsa
        // Use persistence
        .UseEntityFrameworkPersistence(ef => ef.UseSqlite())

        // Register HTTP activities.
        .AddHttpActivities(options => options.BasePath = "/workflows")

        // Register custom activities from this assembly.
        .AddActivitiesFrom<Program>()

        // Register workflows from this assembly.
        .AddWorkflowsFrom<Program>()
    );

var app = builder.Build();

// Install HTTP Endpoint middleware that invokes our workflows that use HTTP Endpoint activities. 
app.UseHttpActivities();
app.UseRouting();
app.UseCors();
app.UseEndpoints(endpoints => endpoints.MapControllers());

// Display a default welcome page in case nothing matches the HTTP request.
app.UseWelcomePage();
app.Run();