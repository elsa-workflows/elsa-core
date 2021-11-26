using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Models;
using MediatR;

namespace Elsa.Scripting.JavaScript.Events
{
    public class RenderingTypeScriptDefinitions : INotification
    {
        internal RenderingTypeScriptDefinitions(WorkflowDefinition? workflowDefinition, Func<Type, string> getTypeScriptType, IntellisenseContext? context, ICollection<string> declaredTypes, StringBuilder output)
        {
            WorkflowDefinition = workflowDefinition;
            GetTypeScriptTypeInternal = getTypeScriptType;
            Context = context;
            DeclaredTypes = declaredTypes;
            Output = output;
        }
        
        private Func<Type, string> GetTypeScriptTypeInternal { get; }
        public IntellisenseContext? Context { get; }
        public StringBuilder Output { get; }
        public WorkflowDefinition? WorkflowDefinition { get; }
        public ICollection<string> DeclaredTypes { get; }

        public string GetTypeScriptType(Type type) => GetTypeScriptTypeInternal(type);
        public string GetTypeScriptType<T>() => GetTypeScriptType(typeof(Type));
    }
}