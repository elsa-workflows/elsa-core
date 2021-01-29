using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class BookmarkHashSpecification : Specification<Bookmark>
    {
        public BookmarkHashSpecification(string hash, string activityType, string? tenantId)
        {
            Hash = hash;
            ActivityType = activityType;
            TenantId = tenantId;
        }

        public string? TenantId { get; set; }
        public string Hash { get; }
        public string ActivityType { get; set; }
        
        public override Expression<Func<Bookmark, bool>> ToExpression() => trigger => 
            trigger.TenantId == TenantId &&
            trigger.ActivityType == ActivityType &&
            trigger.Hash == Hash;
    }
}