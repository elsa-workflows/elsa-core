using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IProcessBuilder
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        int Version { get; }
        ProcessPersistenceBehavior PersistenceBehavior { get; }
        bool DeleteCompletedInstances { get; }
        IProcessBuilder WithId(string value);
        IProcessBuilder WithName(string value);
        IProcessBuilder WithDescription(string value);
        IProcessBuilder WithVersion(int value);
        IProcessBuilder AsSingleton();
        IProcessBuilder AsTransient();
        IProcessBuilder WithRoot<T>(Action<IActivityConfigurator<T>>? setup = default) where T : class, IActivity;
        IActivityConfigurator<T> BuildActivity<T>(Action<IActivityConfigurator<T>>? setupActivity = default) where T : class, IActivity;
        T BuildActivity<T>(Action<T>? setupActivity = default) where T : class, IActivity;
        Process Build();
        Process Build(IProcess process);
        Process Build(Type processType);
        Process Build<T>() where T:IProcess;
    }
}