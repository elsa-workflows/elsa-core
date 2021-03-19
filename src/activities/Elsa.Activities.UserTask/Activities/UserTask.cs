﻿using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.UserTask.Activities
{
    /// <summary>
    /// Stores a set of possible user actions and halts the workflow until one of the actions has been performed.
    /// </summary>
    [Trigger(
        Category = "User Tasks",
        Description = "Triggers when a user action is received.",
        Outcomes = new[] { OutcomeNames.Done, "x => x.state.actions" }
    )]
    public class UserTask : Activity
    {
        private readonly IContentSerializer _serializer;

        [ActivityProperty(UIHint = ActivityPropertyUIHints.DynamicList, Hint = "Provide a list of available actions")]
        public ICollection<string> Actions { get; set; } = new List<string>();

        public UserTask(IContentSerializer serializer)
        {
            _serializer = serializer;
        }

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            var userAction = GetUserAction(context);

            return Actions.Contains(userAction, StringComparer.OrdinalIgnoreCase);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var userAction = GetUserAction(context);
            return Outcome(userAction, userAction);
        }

        private string GetUserAction(ActivityExecutionContext context) => (string)context.Input!;
    }
}