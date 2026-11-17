using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Elsa.Persistence.Elasticsearch.Common;

internal static class ExactQueries
{
    public static void AddKeyword(this ICollection<Query> filters, string field, string? value)
    {
        if (value != null)
            filters.Add(new TermQuery { Field = $"{field}.keyword", Value = value });
    }

    public static void AddKeywords(this ICollection<Query> filters, string field, ICollection<string>? values)
    {
        if (values == null)
            return;

        if (values.Count == 0)
        {
            filters.Add(new MatchNoneQuery());
            return;
        }

        filters.Add(new TermsQuery
        {
            Field = $"{field}.keyword",
            Terms = new TermsQueryField(values.Select(FieldValue.String).ToArray())
        });
    }

    public static void AddDocumentIds(this ICollection<Query> filters, ICollection<string>? ids)
    {
        if (ids == null)
            return;

        if (ids.Count == 0)
        {
            filters.Add(new MatchNoneQuery());
            return;
        }

        filters.Add(new IdsQuery { Values = ids.ToArray() });
    }
}
