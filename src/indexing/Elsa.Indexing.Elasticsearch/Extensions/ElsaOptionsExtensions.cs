using System;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseElasticsearch(this ElsaOptions options, Action<ElsaElasticsearchOptions> configure)
            => options.UseIndexing(indexing => indexing.UseElasticsearch(configure));
    }
}
