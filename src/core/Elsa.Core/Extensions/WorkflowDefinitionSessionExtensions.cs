using System;
using System.Linq.Expressions;
using Elsa.Data;
using Elsa.Models;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionSessionExtensions
    {
        public static void Save(this ISession session, WorkflowDefinition workflowDefinition) =>
            session.Save(workflowDefinition, CollectionNames.WorkflowDefinitions);

        public static IQuery<WorkflowDefinition> QueryWorkflowDefinitions(this ISession session) =>
            session.Query<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);

        public static IQuery<WorkflowDefinition, TIndex> QueryWorkflowDefinitions<TIndex>(this ISession session)
            where TIndex : class, IIndex =>
            session.Query<WorkflowDefinition, TIndex>(CollectionNames.WorkflowDefinitions);
        
        public static IQuery<WorkflowDefinition, TIndex> QueryWorkflowDefinitions<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate)
            where TIndex : class, IIndex =>
            session.Query<WorkflowDefinition, TIndex>(predicate, CollectionNames.WorkflowDefinitions);
    }
}