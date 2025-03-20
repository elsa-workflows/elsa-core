using Elsa.DummyModule;
using Elsa.DummyModule.Features;
using Elsa.Extensions;
using Elsa.Framework.Builders;
using Elsa.Framework.Shells;
using ApplicationBuilder = Elsa.Framework.Builders.ApplicationBuilder;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var elsaBuilder= new ApplicationBuilder();
var appShellBuilder = elsaBuilder.ApplicationShell;
var tenant1Builder = elsaBuilder.AddShell("Tenant1");
appShellBuilder.AddFeature<Dummy1Feature>();
tenant1Builder.AddFeature<Dummy2Feature>();
    
elsaBuilder.Build(services);

var app = builder.Build();
var serviceProvider = app.Services;
var elsaApp = serviceProvider.GetRequiredService<IElsaApplication>();
var shellFactory = serviceProvider.GetRequiredService<IShellFactory>();
var appShellBlueprint = elsaApp.ApplicationShell;
var appShell = shellFactory.CreateShell(appShellBlueprint);
var tenant1ShellBlueprint = elsaApp.ShellBlueprints["Tenant1"];
var tenant1Shell = shellFactory.CreateShell(tenant1ShellBlueprint);

var dummyService1 = appShell.ServiceProvider.GetRequiredService<IDummyService>();
var dummyService2 = tenant1Shell.ServiceProvider.GetRequiredService<IDummyService>();

Console.WriteLine(dummyService1.GetMessage());
Console.WriteLine(dummyService2.GetMessage());

app.MapGet("/", () => "Hello World!");

app.Run();