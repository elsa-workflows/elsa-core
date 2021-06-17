using System;
using System.Linq.Expressions;
using Elsa.Builders;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;

namespace Elsa
{
    public static class SetupActivityExtensions
    {
        public static ISetupActivity<T> WithTransientStorageFor<T, TProperty>(this ISetupActivity<T> builder, Expression<Func<T, TProperty?>> propertyAccessor) where T : IActivity => builder.WithStorageFor(propertyAccessor, TransientWorkflowStorageProvider.ProviderName);
        public static ISetupActivity<T> WithWorkflowInstanceStorageFor<T, TProperty>(this ISetupActivity<T> builder, Expression<Func<T, TProperty?>> propertyAccessor) where T : IActivity => builder.WithStorageFor(propertyAccessor, WorkflowInstanceWorkflowStorageProvider.ProviderName);
        public static ISetupActivity<T> WithBlobStorageFor<T, TProperty>(this ISetupActivity<T> builder, Expression<Func<T, TProperty?>> propertyAccessor) where T : IActivity => builder.WithStorageFor(propertyAccessor, BlobStorageWorkflowStorageProvider.ProviderName);
    }
}