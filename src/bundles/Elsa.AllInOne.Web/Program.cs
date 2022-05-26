using Elsa.Workflows.Api.Extensions;
using Elsa.AspNetCore;
using Elsa.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Hangfire.Implementations;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Quartz.Implementations;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Runtime.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services
    .ConfigureElsa()
    .UseWorkflows()
    .UseRuntime()
    .UseManagement(management => management
        .AddActivity<Sequence>()
        .AddActivity<WriteLine>()
        .AddActivity<ReadLine>()
        .AddActivity<If>()
        .AddActivity<HttpEndpoint>()
        .AddActivity<Flowchart>()
        .AddActivity<Delay>()
        .AddActivity<Elsa.Scheduling.Activities.Timer>()
        .AddActivity<ForEach>()
        .AddActivity<Switch>()
        .AddActivity<RunJavaScript>()
    )
    .UseHttp()
    .Apply();

services
    //.AddProtoActorRuntime()
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices();

// Register scripting languages.
services
    .AddJavaScriptExpressions()
    .AddLiquidExpressions();

// Register serialization configurator for configuring what types to allow to be serialized.
services.AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>();
services.AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>();

// Controllers.
services.AddControllers(mvc => mvc.Conventions.Add(new ApiEndpointAttributeConvention()));

// Razor Pages.
services.AddRazorPages();

// Configure middleware pipeline.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapElsaApiEndpoints();
app.UseHttpActivities();
app.MapRazorPages();

app.Run();