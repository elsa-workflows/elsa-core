using System;
using System.Reflection;
using Elsa.Metadata;

namespace Elsa.Secrets.Enrichers
{
    public interface IActivityInputDescriptorEnricher
    {
        Type ActivityType { get; }
        string PropertyName { get; }

        void Enrich(ActivityInputDescriptor activityInputDescriptor, PropertyInfo propertyInfo);
    }
}