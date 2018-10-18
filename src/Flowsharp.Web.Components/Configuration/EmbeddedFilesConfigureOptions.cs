using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Flowsharp.Web.Components.Configuration
{
    internal class EmbeddedFilesConfigureOptions : IPostConfigureOptions<StaticFileOptions>
    {
        private readonly IHostingEnvironment environment;

        public EmbeddedFilesConfigureOptions(IHostingEnvironment environment)
        {
            this.environment = environment;
        }
        
        public void PostConfigure(string name, StaticFileOptions options)
        {
            options.ContentTypeProvider = options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
            options.FileProvider = options.FileProvider ?? environment.WebRootFileProvider;
            var embeddedFileProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot");
            options.FileProvider = new CompositeFileProvider(options.FileProvider, embeddedFileProvider);
        }
    }
}