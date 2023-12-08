using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Elsa.Workflows.Core.Services;

public partial class PropertyOptionsResolver
{
    public class PropertyUIHandlerResolver : IPropertyUIHandlerResolver
    {
        private IServiceProvider _serviceProvider;

        public PropertyUIHandlerResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async ValueTask<IDictionary<string, object>?> GetUIProperties(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
        {
            var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();

            var result = new Dictionary<string, object>();

            if (inputAttribute?.UIHandler != null)
            {

                foreach(var handlerType in inputAttribute.UIHandler)
                {

                    using var scope = _serviceProvider.CreateScope();
                    var provider = (IPropertyUIHandler)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
                    var properties =  await provider.GetUIProperties(propertyInfo, context, cancellationToken);

                    result.Add(provider.Name, properties);
                }
            }
            return result;
        }
    }
}