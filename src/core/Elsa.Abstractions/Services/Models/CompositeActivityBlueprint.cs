﻿using System.Collections.Generic;

namespace Elsa.Services.Models
{
    public class CompositeActivityBlueprint : ActivityBlueprint, ICompositeActivityBlueprint
    {
        public CompositeActivityBlueprint()
        {
            Activities = new List<IActivityBlueprint>();
            Connections = new List<IConnection>();
            ActivityPropertyProviders = new ActivityPropertyProviders();
        }

        public CompositeActivityBlueprint(
            string id,
            ICompositeActivityBlueprint? parent,
            string? name,
            string? displayName,
            string? description,
            string type,
            bool persistWorkflow,
            bool loadWorkflowContext,
            bool saveWorkflowContext,
            IDictionary<string, string> propertyStorageProviders,
            string? source) : base(id, parent, name, displayName, description, type, persistWorkflow, loadWorkflowContext, saveWorkflowContext, propertyStorageProviders, source)
        {
            Activities = new List<IActivityBlueprint>();
            Connections = new List<IConnection>();
            ActivityPropertyProviders = new ActivityPropertyProviders();
        }

        public ICollection<IActivityBlueprint> Activities { get; set; }
        public ICollection<IConnection> Connections { get; set; }
        public IActivityPropertyProviders ActivityPropertyProviders { get; set; }
    }
}