using Elsa.Jobs.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class JobFactoryExtensions
{
    public static T Create<T>(this IJobFactory factory) where T:IJob => (T)factory.Create(typeof(T));
}