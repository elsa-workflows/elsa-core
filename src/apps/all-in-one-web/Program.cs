using Elsa.Activities;
using Elsa.Api.Extensions;
using Elsa.AspNetCore;
using Elsa.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Management.Extensions;
using Elsa.Management.Serialization;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Activities.Workflows;
using Elsa.Modules.Hangfire.Implementations;
using Elsa.Modules.Http;
using Elsa.Modules.Http.Extensions;
using Elsa.Modules.JavaScript.Activities;
using Elsa.Modules.Quartz.Implementations;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Modules.Scheduling.Extensions;
using Elsa.Persistence.InMemory.Extensions;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Serialization;
using Elsa.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services
    .AddElsa()
    .AddHttpActivityServices()
    //.AddProtoActorRuntime()
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices();

// Register activities available from the designer.
services
    .AddActivity<Sequence>()
    .AddActivity<WriteLine>()
    .AddActivity<ReadLine>()
    .AddActivity<If>()
    .AddActivity<HttpEndpoint>()
    .AddActivity<Flowchart>()
    .AddActivity<Delay>()
    .AddActivity<Elsa.Modules.Scheduling.Activities.Timer>()
    .AddActivity<ForEach>()
    .AddActivity<Switch>()
    .AddActivity<RunJavaScript>()
    ;

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