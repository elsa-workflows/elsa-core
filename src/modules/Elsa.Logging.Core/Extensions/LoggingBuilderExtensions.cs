using Elsa.Logging.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Extensions;

/// <summary>
/// Provides extension methods for configuring logging in an <see cref="ILoggingBuilder"/>.
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds category filters to the logging builder using the specified <see cref="LogSinkOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to configure.</param>
    /// <param name="logSinkOptions">The <see cref="LogSinkOptions"/> containing category filter definitions.</param>
    public static ILoggingBuilder AddCategoryFilters(this ILoggingBuilder builder, LogSinkOptions logSinkOptions)
    {
        return builder.AddCategoryFilters(logSinkOptions.CategoryFilters);
    }

    /// <summary>
    /// Adds category filters to the logging builder using the specified <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to configure.</param>
    /// <param name="categoryFilters">The <see cref="IDictionary{TKey, TValue}"/> containing category filter definitions.</param>
    public static ILoggingBuilder AddCategoryFilters(this ILoggingBuilder builder, IDictionary<string, LogLevel>? categoryFilters)
    {
        if (categoryFilters is null)
            return builder;

        foreach (var filter in categoryFilters)
            builder.AddFilter(filter.Key, filter.Value);

        return builder;
    }
}