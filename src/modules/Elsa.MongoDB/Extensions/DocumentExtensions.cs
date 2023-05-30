using System.Linq.Expressions;
using MongoDB.Driver;

namespace Elsa.MongoDB.Extensions;

public static class DocumentExtensions
{
    public static Expression<Func<TDocument, bool>> BuildExpression<TDocument, TResult>(this TDocument document, Expression<Func<TDocument, TResult>> selector)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        if (selector.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Selector must be a member access expression.", nameof(selector));

        var propertyName = memberExpression.Member.Name;
        
        var property = typeof(TDocument).GetProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"The type {typeof(TDocument)} does not have a '{propertyName}' property.");

        var propertyValue = property.GetValue(document);
        
        var parameter = Expression.Parameter(typeof(TDocument), "x");
        var propertyAccess = Expression.Property(parameter, propertyName);
        var constant = Expression.Constant(propertyValue);
        var body = Expression.Equal(propertyAccess, constant);

        return Expression.Lambda<Func<TDocument, bool>>(body, parameter);
    }
    
    public static Expression<Func<TDocument, bool>> BuildIdFilter<TDocument>(this TDocument document)
    {
        var parameter = Expression.Parameter(typeof(TDocument), "x");
        var property = Expression.Property(parameter, "Id");
        
        var idProperty = typeof(TDocument).GetProperty("Id");
        if (idProperty == null)
        {
            throw new InvalidOperationException($"The type {typeof(TDocument)} does not have an 'Id' property.");
        }
        var id = idProperty.GetValue(document);

        var constant = Expression.Constant(id);
        var body = Expression.Equal(property, constant);

        return Expression.Lambda<Func<TDocument, bool>>(body, parameter);
    }
    
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
}