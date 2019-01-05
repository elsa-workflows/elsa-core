using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Handlers
{
    public abstract class ActivityDescriptorBase<T> : IActivityDescriptor where T : IActivity
    {
        public Type ActivityType => typeof(T);
        public virtual bool IsTrigger => false;
        public abstract LocalizedString Category { get; }
        public virtual LocalizedString DisplayText => new LocalizedString(ActivityType.Name, ActivityType.Name);
        public virtual LocalizedString Description => new LocalizedString("", "");
        protected IEnumerable<LocalizedString> Endpoints(params LocalizedString[] endpoints) => endpoints;
        public virtual IEnumerable<LocalizedString> GetEndpoints(IActivity activity) => GetEndpoints((T) activity);
        protected virtual IEnumerable<LocalizedString> GetEndpoints(T activity) => Endpoints(GetEndpoint());
        protected virtual IEnumerable<LocalizedString> GetEndpoints() => Endpoints(GetEndpoint());
        protected virtual LocalizedString GetEndpoint(IActivity activity) => GetEndpoint((T) activity);
        protected virtual LocalizedString GetEndpoint(T activity) => GetEndpoint();
        protected virtual LocalizedString GetEndpoint() => new LocalizedString("Done", "Done", true);
    }
}