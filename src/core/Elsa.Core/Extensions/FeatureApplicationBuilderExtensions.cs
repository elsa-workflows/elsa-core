using Elsa.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class FeatureApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseElsaFeatures(this IApplicationBuilder applicationBuilder)
        {
            var elsaOptions = applicationBuilder.ApplicationServices.GetRequiredService<ElsaOptions>();

            foreach (var startup in elsaOptions.Startups) 
                startup.ConfigureApp(applicationBuilder);

            return applicationBuilder;
        }
    }
}