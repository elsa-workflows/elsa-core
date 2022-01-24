using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class BookmarkSpecification : Specification<Bookmark>
    {
        public BookmarkSpecification(string activityType, string? tenantId, string? correlationId)
        {
            ActivityType = activityType;
            TenantId = tenantId;
            CorrelationId = correlationId;
        }

        public string? TenantId { get; set; }
        public string? CorrelationId { get; }
        public string ActivityType { get; set; }

        public override Expression<Func<Bookmark, bool>> ToExpression() =>
            CorrelationId == null
                ? bookmark => bookmark.TenantId == TenantId
                              && bookmark.ActivityType == ActivityType
                : bookmark => bookmark.TenantId == TenantId
                              && bookmark.ActivityType == ActivityType
                              && bookmark.CorrelationId == CorrelationId;
    }
}