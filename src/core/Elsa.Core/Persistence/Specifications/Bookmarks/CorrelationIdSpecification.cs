using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class CorrelationIdSpecification : Specification<Bookmark>
    {
        public CorrelationIdSpecification(string correlationId) => CorrelationId = correlationId;
        public string CorrelationId { get; }
        public override Expression<Func<Bookmark, bool>> ToExpression() => trigger => trigger.CorrelationId == CorrelationId;
    }
}