using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowBuilder
    {
        string Id { get; set; }
        IReadOnlyList<IActivityBuilder> Activities { get; }
        IWorkflowBuilder WithId(string id);
        IActivityBuilder Add<T>(Action<T> setupActivity, string id = null) where T : class, IActivity;
        IActivityBuilder StartWith<T>(Action<T> setup = null, string id = null) where T: class, IActivity;
        IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string outcome = null);
        IConnectionBuilder Connect(Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = null);
        WorkflowDefinition Build();
    }
}