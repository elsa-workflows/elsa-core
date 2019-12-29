using System;
using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IProcessBuilder
    {
        string Id { get; set; }
        IActivityBuilder Start { get; }
        IProcessBuilder WithId(string id);
        IFlowchartBuilder WithName(string name);
        IFlowchartBuilder WithDescription(string description);
        IActivityBuilder StartWith<T>(Action<T>? setup = default, string? name = default) where T: class, IActivity;
        Process Build();
    }
}