using System;
using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IFlowchartBuilder
    {
        string Id { get; set; }
        IReadOnlyList<IActivityBuilder> Activities { get; }
        IFlowchartBuilder WithId(string id);
        
        IActivityBuilder Add<T>(Action<T>? setupActivity = default, string? name = default) where T : class, IActivity;
        IActivityBuilder StartWith<T>(Action<T>? setup = default, string? name = default) where T: class, IActivity;
        IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string? outcome = default);
        IConnectionBuilder Connect(Func<IActivityBuilder> source, Func<IActivityBuilder> target, string? outcome = default);
        Activities.Containers.Flowchart Build();
    }
}