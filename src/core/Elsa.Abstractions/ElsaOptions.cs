using Elsa.Builders;
using Elsa.Models;
using Elsa.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa
{
    public class ElsaOptions
    {
        /// <summary>
        /// The set of activities available to workflows.
        /// </summary>
        private readonly TypeList _activities = new();

        /// <summary>
        /// The available programmed workflows
        /// </summary>
        private readonly TypeList _workflows = new();

        public IEnumerable<Type> Activities => _activities.ToList().AsReadOnly();

        public IEnumerable<Type> Workflows => _workflows.ToList().AsReadOnly();

        public int NumberOfRegisteredActivities => _activities.Count;

        public int NumberOfRegisteredWorkflows => _workflows.Count;

        public ElsaOptions RegisterActivity<T>() where T : IActivity, new()
        {
            return RegisterActivity(typeof(T));
        }

        public ElsaOptions RegisterActivity(Type activityType)
        {
            if(IsActivityRegistered(activityType) == false)
            {
                _activities.Add(activityType);
            }

            return this;
        }

        public ElsaOptions UnregisterActivityType<T>() where T : IActivity, new()
        {
            return UnregisterActivityType(typeof(T));
        }

        public ElsaOptions UnregisterActivityType(Type activityType)
        {
            if (IsActivityRegistered(activityType) == false)
            {
                _activities.RemoveWhere(type => type.FullName == activityType.FullName);
            }
            
            return this;
        }

        public bool IsActivityRegistered(Type activityType)
        {
            return _activities.Contains(activityType);
        }

        public ElsaOptions RegisterWorkflow<T>() where T : IWorkflow, new()
        {
            return RegisterWorkflow(typeof(T));
        }

        public ElsaOptions RegisterWorkflow(Type workflowType)
        {
            if (IsWorkflowRegistered(workflowType) == false)
            {
                _workflows.Add(workflowType);
            }

            return this;
        }

        public ElsaOptions UnregisterWorkflowType<T>() where T : IWorkflow, new()
        {
            return UnregisterWorkflowType(typeof(T));
        }

        public ElsaOptions UnregisterWorkflowType(Type workflowType)
        {
            if (IsWorkflowRegistered(workflowType) == false)
            {
                _workflows.RemoveWhere(type => type.FullName == workflowType.FullName);
            }

            return this;
        }

        public bool IsWorkflowRegistered(Type workflowType)
        {
            return _workflows.Contains(workflowType);
        }
    }
}
