using System;
using System.Linq.Expressions;

namespace Elsa.Specifications
{
    public interface IGroupingSpecification<T>
    {
        Expression<Func<T, object>> OrderByExpression { get; }
        SortDirection SortDirection { get; } 
    }

    public class GroupingSpecification<T> : IGroupingSpecification<T>
    {
        public GroupingSpecification(Expression<Func<T, object>> orderByExpression, SortDirection sortDirection)
        {
            OrderByExpression = orderByExpression;
            SortDirection = sortDirection;
        }
        
        public Expression<Func<T, object>> OrderByExpression { get; }
        public SortDirection SortDirection { get; }
    }

    public static class GroupingSpecification
    {
        public static GroupingSpecification<T> OrderBy<T>(Expression<Func<T, object>> expression) => new(expression, SortDirection.Ascending);
        public static GroupingSpecification<T> OrderByDescending<T>(Expression<Func<T, object>> expression) => new(expression, SortDirection.Descending);
    }
}