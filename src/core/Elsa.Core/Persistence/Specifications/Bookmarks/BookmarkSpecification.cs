using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class BookmarkSpecification : Specification<Bookmark>
    {
        public BookmarkSpecification(string activityType, string? tenantId)
        {
            ActivityType = activityType;
            TenantId = tenantId;
        }

        public string? TenantId { get; set; }
        public string ActivityType { get; set; }
        
        public override Expression<Func<Bookmark, bool>> ToExpression() => trigger => 
            trigger.TenantId == TenantId &&
            trigger.ActivityType == ActivityType;
    }
}