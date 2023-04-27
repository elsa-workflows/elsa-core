using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services.AddElsa(elsa => elsa
    // Add the Fluent Storage workflow definition provider.
    .UseFluentStorageProvider()

    // Expose API endpoints.
    .UseWorkflowsApi()

    // Configure identity so that we can create a default admin user.
    .UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    })

    // Use default authentication (JWT).
    .UseDefaultAuthentication(auth => auth.UseAdminApiKey())
);

// Configure middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.Run();