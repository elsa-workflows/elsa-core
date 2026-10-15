using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Elsa.Studio.BlazorWasm.Client.Bootstrap
{
    public static class Core
    {
        public static void SetupCore(this WebAssemblyHostBuilder builder)
        {
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
            builder.RootComponents.RegisterCustomElsaStudioElements();

            builder.Services.AddCore();
            builder.Services.AddShell();
        }
    }
}
