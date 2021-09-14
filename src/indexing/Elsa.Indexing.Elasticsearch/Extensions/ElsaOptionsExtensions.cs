using System;
using Elsa.Options;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptionsBuilder UseElasticsearch(this ElsaOptionsBuilder options, Action<ElsaElasticsearchOptions> configure)
            => options.UseIndexing(indexing => indexing.UseElasticsearch(configure));
    }
}
