using System;
using System.Reflection;
using MediatR.Registration;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaOptions UseIndexing(this ElsaOptions options, Action<ElsaIndexingOptions> configure)
        {
            var indexingOptions = new ElsaIndexingOptions(options.Services);
            configure.Invoke(indexingOptions);

            ServiceRegistrar.AddMediatRClasses(options.Services, new[] { Assembly.GetExecutingAssembly() });

            return options;
        }
    }
}