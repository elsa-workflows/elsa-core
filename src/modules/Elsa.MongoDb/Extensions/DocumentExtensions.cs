using System.Linq.Expressions;
using MongoDB.Driver;

namespace Elsa.MongoDb.Extensions;

public static class DocumentExtensions
{
    public static Expression<Func<TDocument, bool>> BuildExpression<TDocument, TResult>(this TDocument document, Expression<Func<TDocument, TResult>> selector)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        if (selector.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Selector must be a member expression.", nameof(selector));

        var propertyName = memberExpression.Member.Name;

        return document.BuildFilter(propertyName);
    }
    
    public static Expression<Func<TDocument, bool>> BuildIdFilter<TDocument>(this TDocument document) => 
        document.BuildFilter("Id");

    public static FilterDefinition<TDocument> BuildIdFilterForList<TDocument>(this IEnumerable<TDocument> documents)
    {
        var propertyName = "Id";
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