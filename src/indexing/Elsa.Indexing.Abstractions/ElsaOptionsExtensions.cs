using System;

using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Indexing
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions AddIndexing(this ElsaOptions options, Action<ElsaIndexingOptions>? configure = default)
        {
            var indexingOptions = new ElsaIndexingOptions(options.Services);
            configure?.Invoke(indexingOptions);

            options.Services
                .AddSingleton(options);

            return options;
        }
    }
}
