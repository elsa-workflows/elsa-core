using Elsa.ReactStudio.Host;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// This executable exists specifically to host the opt-in Preview.
app.MapReactStudio(options =>
{
    options.Path = builder.Configuration["ReactStudio:Path"] ?? "/studio-preview";
    options.ConfigurationFile = builder.Configuration["ReactStudio:ConfigurationFile"] ?? Path.Combine(AppContext.BaseDirectory, "studio.config.json");
});
app.Run();

public partial class Program;
