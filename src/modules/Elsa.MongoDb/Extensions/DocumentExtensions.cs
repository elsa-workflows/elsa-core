using System.Linq.Expressions;
using MongoDB.Driver;

namespace Elsa.MongoDb.Extensions;

/// <summary>
/// Provides extension methods for building expressions.
/// </summary>
public static class DocumentExtensions
{
    /// <summary>
    /// Builds a filter expression for the specified property name.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="selector">The property selector.</param>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the property.</typeparam>
    /// <exception cref="ArgumentNullException">The document is null.</exception>
    /// <exception cref="ArgumentException">The selector is not a member expression.</exception>
    public static Expression<Func<TDocument, bool>> BuildExpression<TDocument, TResult>(this TDocument document, Expression<Func<TDocument, TResult>> selector)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        if (selector.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Selector must be a member expression.", nameof(selector));

        var propertyName = memberExpression.Member.Name;

        return document.BuildFilter(propertyName);
    }
    
    /// <summary>
    /// Builds a filter expression for the Id property.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static Expression<Func<TDocument, bool>> BuildIdFilter<TDocument>(this TDocument document) => 
        document.BuildFilter("Id");

    /// <summary>
    /// Builds a filter expression for the Id property name of the specified documents.
    /// </summary>
    /// <param name="documents">The documents.</param>
    /// <param name="key">The key.</param>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <exception cref="InvalidOperationException">The type does not have an Id property.</exception>
    public static FilterDefinition<TDocument> BuildIdFilterForList<TDocument>(this IEnumerable<TDocument> documents, string key = "Id")
    {
        var propertyName = key;
        var idProperty = typeof(TDocument).GetProperty(propertyName);
        if (idProperty == null)
        {
            throw new InvalidOperationException($"The type {typeof(TDocument)} does not have a '{propertyName}' property.");
        }

        var ids = documents.Select(document => idProperty.GetValue(document)).ToList();
        return Builders<TDocument>.Filter.In(propertyName, ids);
    }
    
    private static Expression<Func<TDocument, bool>> BuildFilter<TDocument>(this TDocument document, string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TDocument), "x");
        var expressionProperty = Expression.Property(parameter, propertyName);
        
        var prop = typeof(TDocument).GetProperty(propertyName);
        if (prop == null)
        {
            throw new InvalidOperationException($"The type {typeof(TDocument)} does not have an {propertyName} property.");
        }
        var propValue = prop.GetValue(document);

        var constant = Expression.Constant(propValue);
        var body = Expression.Equal(expressionProperty, constant);

        return Expression.Lambda<Func<TDocument, bool>>(body, parameter);
    }
}