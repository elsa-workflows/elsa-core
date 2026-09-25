using Elsa.Studio.Host.HostedWasm.Middleware;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseRouting();
app.UseCors();

// Insert this BEFORE UseStaticFiles middleware.
app.UseMiddleware<WasmAssetsRewritingMiddleware>();

// Use Static Files instead of MapStaticAssets.
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            // Add custom MIME type mappings for WASM
            [".dat"] = "application/octet-stream"
        }
    }
});
app.MapFallbackToPage("/_Host");

app.Run();