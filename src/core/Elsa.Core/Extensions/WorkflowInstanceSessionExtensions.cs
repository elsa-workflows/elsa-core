using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Persistence;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceSessionExtensions
    {
        public static void Save(this ISession session, WorkflowInstance workflowInstance) =>
            session.Save(workflowInstance, CollectionNames.WorkflowInstances);
        
        public static void Delete(this ISession session, WorkflowInstance workflowInstance) =>
            session.Delete(workflowInstance, CollectionNames.WorkflowInstances);

        public static IQuery<WorkflowInstance> QueryWorkflowInstances(this ISession session) =>
            session.Query<WorkflowInstance>(CollectionNames.WorkflowInstances);

        public static IQuery<WorkflowInstance, TIndex> QueryWorkflowInstances<TIndex>(this ISession session)
            where TIndex : class, IIndex =>
            session.Query<WorkflowInstance>(CollectionNames.WorkflowInstances).With<TIndex>();
        
        public static IQuery<WorkflowInstance, TIndex> QueryWorkflowInstances<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate)
            where TIndex : class, IIndex =>
            session.Query<WorkflowInstance>(CollectionNames.WorkflowInstances).With(predicate);
    }
}