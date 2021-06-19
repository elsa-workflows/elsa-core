using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class FeatureApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseElsaFeatures(this IApplicationBuilder applicationBuilder)
        {
            var elsaOptions = applicationBuilder.ApplicationServices.GetRequiredService<ElsaOptions>();

            foreach (var (type, method) in elsaOptions.ConfigureAppMethods)
            {
                var methodParams = method.GetParameters();
                var parametersArray = methodParams.Select(x => x.ParameterType == typeof(IApplicationBuilder) ? (object) applicationBuilder : throw new ArgumentException()).ToArray();
                var classInstance = Activator.CreateInstance(type, null);
                method.Invoke(classInstance, parametersArray);
            }

            return applicationBuilder;
        }
    }
}