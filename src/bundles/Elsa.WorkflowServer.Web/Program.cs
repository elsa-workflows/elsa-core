using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.ActivityDefinitions.EntityFrameworkCore.Extensions;
using Elsa.ActivityDefinitions.EntityFrameworkCore.Sqlite;
using Elsa.Api.Common;
using Elsa.Api.Common.Features;
using Elsa.Api.Common.Options;
using Elsa.AspNetCore.Extensions;
using Elsa.Extensions;
using Elsa.Features.Extensions;
using Elsa.Hangfire.Implementations;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Labels.EntityFrameworkCore.Extensions;
using Elsa.Labels.EntityFrameworkCore.Sqlite;
using Elsa.Labels.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Quartz.Implementations;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.WorkflowServer.Web.Implementations;
using FastEndpoints;
using FastEndpoints.Security;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var accessTokenOptions = new AccessTokenOptions();
configuration.GetSection("AccessTokens").Bind(accessTokenOptions);

// Global control over Elsa API endpoints security.
ApiSecurityOptions.AllowAnonymous = !builder.Environment.IsProduction();
ApiSecurityOptions.ValidateRoles = builder.Environment.IsProduction();

// Add application-specific services.
services.AddSingleton<CustomCredentialsValidator>();
services.AddSingleton<CustomAccessTokenIssuer>();

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseManagement(management => management
            .AddActivity<Sequence>()
            .AddActivity<WriteLine>()
            .AddActivity<ReadLine>()
            .AddActivity<If>()
            .AddActivity<HttpEndpoint>()
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
        .Use<CommonApiFeature>(feature =>
        {
            feature.TokenSigningKey = accessTokenOptions.SigningKey;
            feature.CredentialsValidator = sp => sp.GetRequiredService<CustomCredentialsValidator>();
            feature.AccessTokenIssuer = sp => sp.GetRequiredService<CustomAccessTokenIssuer>();
        })
        .UseWorkflowPersistence(p => p.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseWorkflowApiEndpoints()
        .UseJavaScript()
        .UseLiquid()
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseCustomActivities(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseHttp()
        .UseMvc()
    );

services
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices()
    ;

services.AddFastEndpoints();
services.AddAuthenticationJWTBearer(accessTokenOptions.SigningKey);
services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Authorization policies.
services.AddAuthorization(options => options.AddPolicy("WorkflowManagerPolicy", policy => policy.RequireAuthenticatedUser()));

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

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

// Map Elsa API endpoint controllers.

// Deprecated.
app.MapManagementApiEndpoints();
app.MapLabelApiEndpoints();

// Use FastEndpoints middleware.

ValueTask<object?> DeserializeRequestAsync(HttpRequest httpRequest, Type modelType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
{
    var serializerOptionsProvider = httpRequest.HttpContext.RequestServices.GetRequiredService<SerializerOptionsProvider>();
    var options = serializerOptionsProvider.CreateApiOptions();

    return serializerContext == null
        ? JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, options, cancellationToken)
        : JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, serializerContext, cancellationToken);
}

Task SerializeRequestAsync(HttpResponse httpResponse, object dto, string contentType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
{
    var serializerOptionsProvider = httpResponse.HttpContext.RequestServices.GetRequiredService<SerializerOptionsProvider>();
    var options = serializerOptionsProvider.CreateApiOptions();

    httpResponse.ContentType = contentType;
    return serializerContext == null
        ? JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto.GetType(), options, cancellationToken)
        : JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto.GetType(), serializerContext, cancellationToken);
}

app.UseFastEndpoints(config =>
{
    config.RoutingOptions = routing => routing.Prefix = "elsa/api";
    config.RequestDeserializer = DeserializeRequestAsync;
    config.ResponseSerializer = SerializeRequestAsync;
});

// Register Elsa middleware.
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();