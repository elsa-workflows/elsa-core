using Elsa.Models;
using Elsa.Persistence.Specifications;
using System;
using System.Linq.Expressions;

namespace Elsa.Retention.Specifications
{
    public class WorkflowStatusFilterSpecification : Specification<WorkflowInstance>
    {
        public WorkflowStatusFilterSpecification(params WorkflowStatus[] statuses)
        {
            Statuses = statuses;
        }

        public WorkflowStatus[] Statuses { get; }

        public override Expression<Func<WorkflowInstance, bool>> ToExpression()
        {

            Expression exp = null;
            var i = 0;
            var param = Expression.Parameter(typeof(WorkflowInstance), "instance");

            foreach (var status in Statuses)
            {
                var equality = Expression.Equal(Expression.Property(param, "WorkflowStatus"), Expression.Constant(status));
                if (i == 0)
                    exp = equality;
                else
                    exp = Expression.Or(exp, equality);
                i++;
            }

            Expression<Func<WorkflowInstance, bool>> lambda =
                Expression.Lambda<Func<WorkflowInstance, bool>>(
                    exp,
                    new ParameterExpression[] { param });

            return lambda;
        }


    }
}
