using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JintEvaluationResultConverterFactory
    {
        private readonly IServiceProvider _serviceProvider;
        
        public JintEvaluationResultConverterFactory(IServiceProvider serviceProvider) => this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public IConvertsJintEvaluationResult GetConverter()
        {
            IConvertsJintEvaluationResult service;

            // Builds a chain-of-responsibility service
            // Note:  The order in which these classes execute is bottom-to-top

            service = GetConvertChangeTypeService();
            service = GetPlainObjectService(service);
            service = GetEnumerableConvertingService(service);
            service = GetExpandoConvertingService(service);
            service = GetTypeConverterConvertingService(service);
            service = GetNullConvertingService(service);

            return service;
        }

        static IConvertsJintEvaluationResult GetConvertChangeTypeService() => new ConvertChangeTypeResultConverter();

        static IConvertsJintEvaluationResult GetPlainObjectService(IConvertsJintEvaluationResult wrapped) => new PlainObjectResultConverter(wrapped);

        IConvertsJintEvaluationResult GetEnumerableConvertingService(IConvertsJintEvaluationResult wrapped) => new EnumerableResultConverter(wrapped);

        IConvertsJintEvaluationResult GetExpandoConvertingService(IConvertsJintEvaluationResult wrapped)
        {
            var enumerableConverter = _serviceProvider.GetRequiredService<IConvertsEnumerableToObject>();
            return new ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter(enumerableConverter, wrapped);
        }

        static IConvertsJintEvaluationResult GetTypeConverterConvertingService(IConvertsJintEvaluationResult wrapped) => new TypeConverterResultConverter(wrapped);

        static IConvertsJintEvaluationResult GetNullConvertingService(IConvertsJintEvaluationResult wrapped) => new NullResultConverter(wrapped);
    }
}