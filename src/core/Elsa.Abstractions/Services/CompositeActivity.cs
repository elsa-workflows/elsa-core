﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public class CompositeActivity : Activity
    {
        internal const string Enter = "Enter";
        
        public virtual void Build(ICompositeActivityBuilder activity)
        {
        }

        public bool IsScheduled
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        public override JObject Data
        {
            get
            {
                // When executing a composite activity's workflow, child activities might try and reference the composite activity's "input" properties.
                // Out of the box, that doesn't work, since "this" points to a new instance with an empty Data object.
                // Instead, we need to capture the Data object of the composite activity (the parent of the currently executing child activity) using the ambient activity execution context.
                if (AmbientActivityExecutionContext.Current == null) 
                    return base.Data;
                
                var context = AmbientActivityExecutionContext.Current;
                    
                // Check if the currently executing activity is something other than this composite activity.
                if (context.ActivityBlueprint.Type == Type) 
                    return base.Data;
                
                // A child activity is attempting to retrieve property data from its parent composite activity.
                var parentId = context.ActivityBlueprint.Parent!.Id;
                return context.WorkflowInstance.ActivityData.GetItem(parentId)!;
            }
            set => base.Data = value;
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (!IsScheduled)
            {
                context.WorkflowInstance.Scopes.Push(new ActivityScope(Id));
                IsScheduled = true;
                await OnEnterAsync(context);
                return Outcome(Enter);
            }

            IsScheduled = false;
            await OnExitAsync(context);

            var finishOutput = context.Input as FinishOutput;
            var outcomes = new List<string> { OutcomeNames.Done };
            var output = default(object?);

            if (finishOutput != null)
            {
                outcomes.AddRange(finishOutput.Outcomes);
                output = finishOutput.Output;
            }
            
            return Combine(Outcomes(outcomes), Output(output));
        }

        protected virtual ValueTask OnEnterAsync(ActivityExecutionContext context)
        {
            OnEnter(context);
            return new();
        }

        protected virtual ValueTask OnExitAsync(ActivityExecutionContext context)
        {
            OnExit(context);
            return new();
        }

        protected virtual void OnEnter(ActivityExecutionContext context)
        {
        }
        
        protected virtual void OnExit(ActivityExecutionContext context)
        {
        }
    }
}