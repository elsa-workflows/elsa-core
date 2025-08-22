using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddCategoryFilters(this ILoggingBuilder builder, SinkOptions.SinkOptions sinkOptions)
    {
        return builder.AddCategoryFilters(sinkOptions.CategoryFilters);
    }

    public static ILoggingBuilder AddCategoryFilters(this ILoggingBuilder builder, IDictionary<string, LogLevel>? categoryFilters)
    {
        if (categoryFilters is null)
            return builder;

        foreach (var filter in categoryFilters)
            builder.AddFilter(filter.Key, filter.Value);

        return builder;
    }
}