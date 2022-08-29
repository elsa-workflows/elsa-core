using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elsa;
using Elsa.ActivityDefinitions.EntityFrameworkCore.Extensions;
using Elsa.ActivityDefinitions.EntityFrameworkCore.Sqlite;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Activities.Extensions;
using Elsa.Jobs.Activities.Implementations;
using Elsa.Jobs.Activities.Services;
using Elsa.Labels.EntityFrameworkCore.Extensions;
using Elsa.Labels.EntityFrameworkCore.Sqlite;
using Elsa.Labels.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.WorkflowServer.Web.Jobs;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

EndpointSecurityOptions.DisableSecurity();

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseManagement(management => management
            .AddActivity<WriteLine>()
            .AddActivity<ReadLine>()
            .AddActivity<If>()
            .AddActivity<HttpEndpoint>()
            .AddActivity<WriteHttpResponse>()
            .AddActivity<Flowchart>()
            .AddActivity<FlowDecision>()
            .AddActivity<FlowSwitch>()
            .AddActivity<Elsa.Scheduling.Activities.Delay>()
            .AddActivity<Elsa.Scheduling.Activities.Timer>()
            .AddActivity<ForEach>()
            .AddActivity<Switch>()
            .AddActivity<RunJavaScript>()
            .AddActivity<Event>()
        )
        .UseJobActivities()
        .UseScheduling()
        .UseWorkflowPersistence(p => p.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseWorkflowApiEndpoints()
        .UseJavaScript()
        .UseLiquid()
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseCustomActivities(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseHttp()
    );

services.AddFastEndpoints();
services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Authentication & Authorization.
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var claims = 
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKeyValidator = (key, token, parameters) => true,
            ValidateIssuerSigningKey = false,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateTokenReplay = false,
            ValidateActor = false,
            RoleClaimType = ClaimTypes.Role,
            TokenReplayValidator = (time, token, parameters) => true, 
            SignatureValidator = (token, parameters) =>
            {
                return new JwtSecurityToken(token);
            }, 
        };
        options.SecurityTokenValidators.Clear();
        options.SecurityTokenValidators.Add(new CustomValidator());
    });

services.AddAuthorization(options => options.AddPolicy("WorkflowManagerPolicy", policy => policy.RequireAuthenticatedUser()));

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

// Register a dummy job for demo purposes.
var jobRegistry = serviceProvider.GetRequiredService<IJobRegistry>();
jobRegistry.Add(typeof(IndexBlockchainJob));

// Update activity providers.
var activityRegistryPopulator = serviceProvider.GetRequiredService<IActivityRegistryPopulator>();
activityRegistryPopulator.PopulateRegistryAsync(typeof(JobActivityProvider));

// Configure workflow engine execution pipeline.
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline =>
    pipeline
        .UseWorkflowExecutionEvents()
        .UseWorkflowExecutionLogPersistence()
        .UsePersistence()
        .UseWorkflowContexts()
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

app.UseAuthentication();
app.UseAuthorization();

// Register Elsa middleware.
app.UseElsaFastEndpoints();
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();

public class CustomValidator : ISecurityTokenValidator
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public CustomValidator()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public bool CanReadToken(string securityToken)
    {
        return true;
    }

    public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
    {
        var principal = _tokenHandler.ValidateToken(securityToken, validationParameters, out validatedToken);

        return principal;
    }

    public bool CanValidateToken => true;
    public int MaximumTokenSizeInBytes { get; set; } = TokenValidationParameters.DefaultMaximumTokenSizeInBytes;
}