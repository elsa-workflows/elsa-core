using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

namespace Elsa.Persistence.Specifications.Bookmarks;

public class BookmarkTypeAndWorkflowInstanceSpecification : Specification<Bookmark>
{
    public BookmarkTypeAndWorkflowInstanceSpecification(string modelType, string workflowInstanceId)
    {
        ModelType = modelType;
        WorkflowInstanceId = workflowInstanceId;
    }

    public string ModelType { get; }
    public string WorkflowInstanceId { get; }

    public override Expression<Func<Bookmark, bool>> ToExpression() => bookmark => bookmark.WorkflowInstanceId == WorkflowInstanceId && bookmark.ModelType == ModelType;
    
    public static BookmarkTypeAndWorkflowInstanceSpecification For<T>(string workflowInstanceId) where T : IBookmark => new(typeof(T).GetSimpleAssemblyQualifiedName(), workflowInstanceId);
}