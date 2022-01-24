using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

namespace Elsa.Persistence.Specifications.Bookmarks;

public class BookmarkTypeSpecification : Specification<Bookmark>
{
    public BookmarkTypeSpecification(string modelType, string? tenantId)
    {
        ModelType = modelType;
        TenantId = tenantId;
    }

    public string ModelType { get; }
    public string? TenantId { get; }

    public override Expression<Func<Bookmark, bool>> ToExpression() => bookmark => bookmark.TenantId == TenantId && bookmark.ModelType == ModelType;
    
    public static BookmarkTypeSpecification For<T>(string? tenantId = default) where T : IBookmark => new(typeof(T).GetSimpleAssemblyQualifiedName(), tenantId);
}