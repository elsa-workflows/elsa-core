using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Elsa.Dashboard.Options
{
    internal class StaticAssetsConfigureOptions : IPostConfigureOptions<StaticFileOptions>
    {
        private readonly IHostingEnvironment environment;

        public StaticAssetsConfigureOptions(IHostingEnvironment environment)
        {
            this.environment = environment;
        }

        public void PostConfigure(string name, StaticFileOptions options)
        {
            options.ContentTypeProvider = options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();

            if (options.FileProvider == null && environment.WebRootFileProvider == null)
            {
                throw new InvalidOperationException("Missing FileProvider.");
            }

            options.FileProvider = options.FileProvider ?? environment.WebRootFileProvider;

            var embeddedFileProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot");

            options.FileProvider = new CompositeFileProvider(options.FileProvider, embeddedFileProvider);
        }
    }
}