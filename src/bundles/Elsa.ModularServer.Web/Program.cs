using Elsa.Extensions;
using Elsa.Framework.Builders;
using Elsa.Workflows.Features;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var elsaBuilder= new ElsaBuilder();
elsaBuilder.AddShell().AddFeature<WorkflowsShellFeature>();
elsaBuilder.AddShell().AddFeature<WorkflowsShellFeature>();
    
elsaBuilder.Build(services);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();