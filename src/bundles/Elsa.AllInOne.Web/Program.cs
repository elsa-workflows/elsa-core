using System.Text.Json.Serialization;
using Elsa.Workflows.Api.Extensions;
using Elsa.AspNetCore.Extensions;
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
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Runtime.Extensions;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services
    .AddElsa(elsa => elsa
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
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
        .UseCustomActivities()
        .UseMvc()
    );

services
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices();

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
app.MapManagementApiEndpoints();
app.UseHttpActivities();
app.UseFastEndpoints();
app.MapRazorPages();

app.Run();