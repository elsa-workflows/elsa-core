using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services.AddElsa(elsa => elsa
    .AddWorkflowsFrom<Program>()
        
    // Enable Elsa HTTP module (for HTTP related activities). 
    .UseScheduling()
);

// Configure middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Start accepting requests.
app.Run();