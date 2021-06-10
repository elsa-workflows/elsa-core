using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    /// <summary>
    /// Inject scoped services into singletons.
    /// </summary>
    public class Scoped<T> where T : notnull
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public Scoped(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public async Task<TResult> UseServiceAsync<TResult>(Func<T, Task<TResult>> scopedAction)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            return await scopedAction(service);
        }

        public async Task UseServiceAsync(Func<T, Task> scopedAction)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await scopedAction(service);
        }

        public TResult UseService<TResult>(Func<T, TResult> scopedAction)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            return scopedAction(service);
        }

        public void UseService(Action<T> scopedAction)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            scopedAction(service);
        }
    }
}