using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

Func<IContainer, MultitenantContainer> accessor = container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container);

builder.Host.UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(accessor));

services
    .AddElsaServices()
    .AddElsaApiEndpoints()
    .AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));

builder.Host.ConfigureContainer<ContainerBuilder>((ctx, builder) =>
{
    var sc = new ServiceCollection();

    builder
       .ConfigureElsaServices(sc,
            options => options
                .AddConsoleActivities()
                .AddHttpActivities(httpOptions => configuration.GetSection("Elsa:Http").Bind(httpOptions))
                .AddWorkflowsFrom<Program>());

    builder.Populate(sc);
});

var app = builder.Build();

app.UseCors();
app.UseRouting();
app.UseHttpActivities();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseWelcomePage();

app.Run();