using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Stability
{
    public class LoopHandlerProvider : ILoopHandlerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        public LoopHandlerProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
        public ILoopHandler GetHandler(Type type) => (ILoopHandler)_serviceProvider.GetRequiredService(type);
    }
}