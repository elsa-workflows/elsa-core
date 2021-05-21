using System.Buffers;
using Elsa.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Elsa.Server.Api.ModelBinders
{
    public class ElsaJsonBodyModelBinder : BodyModelBinder
    {
        public ElsaJsonBodyModelBinder(
            ILoggerFactory loggerFactory,
            ArrayPool<char> charPool,
            IHttpRequestStreamReaderFactory readerFactory,
            ObjectPoolProvider objectPoolProvider,
            IOptions<MvcOptions> mvcOptions,
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
            : base(GetInputFormatters(loggerFactory, charPool, objectPoolProvider, mvcOptions, jsonOptions), readerFactory)
        {
        }
 
        private static IInputFormatter[] GetInputFormatters(
            ILoggerFactory loggerFactory,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            IOptions<MvcOptions> mvcOptions,
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
        {
            var jsonOptionsValue = jsonOptions.Value;
            var serializerSettings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();
            
            return new IInputFormatter[]
            {
                new NewtonsoftJsonInputFormatter(
                    loggerFactory.CreateLogger<ElsaJsonBodyModelBinder>(),
                    serializerSettings,
                    charPool,
                    objectPoolProvider,
                    mvcOptions.Value,
                    jsonOptionsValue)
            };
        }
    }
}