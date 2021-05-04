using System;
using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Scripting.JavaScript.Events
{
    public class CollectingTypeScriptDefinitionTypes : INotification
    {
        internal CollectingTypeScriptDefinitionTypes(WorkflowDefinition? workflowDefinition, Action<IEnumerable<Type>> collectTypes)
        {
            WorkflowDefinition = workflowDefinition;
            CollectTypes = collectTypes;
        }

        private Action<IEnumerable<Type>> CollectTypes { get; }
        public WorkflowDefinition? WorkflowDefinition { get; }

        public void CollectType<T>() => CollectType(typeof(T));
        public void CollectType(Type type) => CollectTypes(new[] { type });
    }
}