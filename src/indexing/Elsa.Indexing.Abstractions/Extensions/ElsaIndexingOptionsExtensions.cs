using System;
using System.Reflection;

using MediatR;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaOptions UseIndexing(this ElsaOptions options, Action<ElsaIndexingOptions> configure)
        {
            var indexingOptions = new ElsaIndexingOptions(options.Services);
            configure.Invoke(indexingOptions);

            options.Services.AddMediatR(Assembly.GetExecutingAssembly());

            return options;
        }
    }
}
