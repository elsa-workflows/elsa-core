using Elsa.Jobs.Services;

namespace Elsa.Jobs.Extensions;

public static class JobFactoryExtensions
{
    public static T Create<T>(this IJobFactory factory) where T:IJob => (T)factory.Create(typeof(T));
}