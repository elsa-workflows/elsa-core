using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowBuilder
    {
        string Id { get; set; }
        IReadOnlyList<IActivityBuilder> Activities { get; }
        IWorkflowBuilder WithId(string id);
        IWorkflowBuilder WithVersion(int version);
        IWorkflowBuilder WithName(string name);
        IWorkflowBuilder WithDescription(string description);
        IWorkflowBuilder AsSingleton(bool value = true);
        IActivityBuilder Add<T>(Action<T> setupActivity = default, string name = default) where T : class, IActivity;
        IActivityBuilder StartWith<T>(Action<T> setup = default, string name = default) where T: class, IActivity;
        IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string outcome = default);
        IConnectionBuilder Connect(Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = default);
        WorkflowDefinitionVersion Build();
    }
}