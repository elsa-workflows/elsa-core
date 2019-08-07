using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace Elsa.Dashboard.Middleware
{
    public class DashboardMiddleware
    {
        public DashboardMiddleware(RequestDelegate _)
        {
            
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            var assembly = typeof(DashboardMiddleware).Assembly;
            using (var resource = new StreamReader(assembly.GetManifestResourceStream("Elsa.Dashboard.wwwroot.index.html")))
            {
                var index = await resource.ReadToEndAsync();
                await context.Response.WriteAsync(index);
            }
        }
    }
}