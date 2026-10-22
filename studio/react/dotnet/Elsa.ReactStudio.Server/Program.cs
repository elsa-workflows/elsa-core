using Elsa.ReactStudio;

var builder = WebApplication.CreateBuilder(args);
// This executable exists specifically to host the opt-in Preview.
builder.AddReactStudioPreview();
var app = builder.Build();
app.UseRouting();
app.MapReactStudioPreview();
app.Run();

public partial class Program;
