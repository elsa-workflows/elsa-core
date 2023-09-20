using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services.AddElsa(elsa => elsa
    .AddWorkflowsFrom<Program>()
        
    // Enable Elsa HTTP module (for HTTP related activities). 
    .UseHttp()
);

// Configure middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Add Elsa HTTP middleware (to handle requests mapped to HTTP Endpoint activities)
app.UseWorkflows();

// Start accepting requests.
app.Run();