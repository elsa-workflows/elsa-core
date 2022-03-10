using System;
using System.Reflection;
using Elsa.Options;
using MediatR;
using MediatR.Registration;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaOptionsBuilder UseIndexing(this ElsaOptionsBuilder options, Action<ElsaIndexingOptions> configure)
        {
            var indexingOptions = new ElsaIndexingOptions(options.Services);
            configure.Invoke(indexingOptions);

            ServiceRegistrar.AddMediatRClasses(options.Services, new[] { Assembly.GetExecutingAssembly() }, new MediatRServiceConfiguration());

            return options;
        }
    }
}