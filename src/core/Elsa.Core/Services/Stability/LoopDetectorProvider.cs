using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Stability
{
    public class LoopDetectorProvider : ILoopDetectorProvider
    {
        private readonly IServiceProvider _serviceProvider;
        public LoopDetectorProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
        public ILoopDetector GetDetector(Type type) => (ILoopDetector)_serviceProvider.GetRequiredService(type);
    }
}