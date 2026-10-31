using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.WebHost.UseStaticWebAssets();

services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseRouting();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

app.Run();