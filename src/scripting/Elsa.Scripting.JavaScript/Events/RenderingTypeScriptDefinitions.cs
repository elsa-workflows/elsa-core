﻿using System;
using System.Text;
using Elsa.Models;
using MediatR;

namespace Elsa.Scripting.JavaScript.Events
{
    public class RenderingTypeScriptDefinitions : INotification
    {
        internal RenderingTypeScriptDefinitions(WorkflowDefinition? workflowDefinition, Func<Type, string> getTypeScriptType, string? context, StringBuilder output)
        {
            WorkflowDefinition = workflowDefinition;
            GetTypeScriptTypeInternal = getTypeScriptType;
            Context = context;
            Output = output;
        }
        
        private Func<Type, string> GetTypeScriptTypeInternal { get; }
        public string? Context { get; }
        public StringBuilder Output { get; }
        public WorkflowDefinition? WorkflowDefinition { get; }

        public string GetTypeScriptType(Type type) => GetTypeScriptTypeInternal(type);
        public string GetTypeScriptType<T>() => GetTypeScriptType(typeof(Type));
    }
}