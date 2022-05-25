using Elsa.Activities;
using Elsa.Extensions;
using Elsa.Hangfire.Implementations;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.Quartz.Implementations;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Extensions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Serialization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services.
services
    .AddElsa()
    .AddHttpActivityServices()
    .AddProtoActorRuntime()
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
    .AddActivity<Elsa.Scheduling.Activities.Timer>()
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