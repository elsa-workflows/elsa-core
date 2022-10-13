using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.Jobs.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Management.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services.
services
    .AddElsa(elsa => elsa
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
            .AddActivity<RunJavaScript>())
        .UseJobs()
        .UseScheduling()
        .UseWorkflowApiEndpoints()
        .UseHttp()
    );

// Razor Pages.
services.AddRazorPages();

services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );

});

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
app.UseHttpActivities();
app.MapRazorPages();

app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // allow any origin 
    .AllowCredentials());

app.Run();