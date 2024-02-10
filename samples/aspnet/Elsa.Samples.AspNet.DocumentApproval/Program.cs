using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services.AddElsa(elsa => elsa
    .AddWorkflowsFrom<Program>()
        
    // Add FastEndpoints.
    .UseWorkflowsApi()
    
    // Enable SAS tokens.
    .UseSasTokens()
    
    // Enable Elsa HTTP module (for HTTP related activities). 
    .UseHttp(http => http.ConfigureHttpOptions = options =>
    {
        options.BaseUrl = new Uri("https://localhost:5001");
    })
);

// Configure middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Expose Elsa APIs.
app.UseWorkflowsApi();

// Add Elsa HTTP middleware (to handle requests mapped to HTTP Endpoint activities)
app.UseWorkflows();

// Start accepting requests.
app.Run();