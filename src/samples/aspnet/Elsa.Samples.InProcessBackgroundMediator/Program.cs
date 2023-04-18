using Elsa.Samples.InProcessBackgroundMediator.Extensions;
using Elsa.Samples.InProcessBackgroundMediator.HostedServices;
using Elsa.Samples.InProcessBackgroundMediator.Implementations;
using Elsa.Samples.InProcessBackgroundMediator.Services;
using MediatR;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionExtensions).Assembly))
    .AddSingleton<IBackgroundEventPublisher, BackgroundEventPublisher>()
    .AddHostedService<BackgroundEventPublisherHostedService>()
    .CreateChannel<INotification>();

services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();