using System;
using System.Reflection;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Enrichers
{
    public abstract class BaseActivityInputDescriptorEnricher : IActivityInputDescriptorEnricher
    {
        private readonly IServiceProvider _serviceProvider;

        public BaseActivityInputDescriptorEnricher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public abstract Type ActivityType { get; }
        public abstract string PropertyName { get; }

        public abstract Type OptionsProvider { get; }

        public void Enrich(ActivityInputDescriptor activityInputDescriptor, PropertyInfo propertyInfo)
        {
            if (activityInputDescriptor == null) return;

            activityInputDescriptor.UIHint = ActivityInputUIHints.Dropdown;
            activityInputDescriptor.DefaultSyntax = SyntaxNames.Secret;

            var provider = (IActivityPropertyOptionsProvider)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, OptionsProvider);
            activityInputDescriptor.Options = provider.GetOptions(propertyInfo);
        }
    }
}
