using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Api.Attributes
{
    /// <summary>
    /// Mark a controller to use Newtonsoft as the formatter
    /// </summary>
    public class NewtonsoftJsonFormatterAttribute : ActionFilterAttribute, IControllerModelConvention, IActionModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                Apply(action);
            }
        }

        public void Apply(ActionModel action)
        {
            // Set the model binder to NewtonsoftJsonBodyModelBinder for parameters that are bound to the request body.
            var parameters = action.Parameters.Where(p => p.BindingInfo?.BindingSource == BindingSource.Body);
            foreach (var p in parameters)
            {
                if (p.BindingInfo != null)
                {
                    p.BindingInfo.BinderType = typeof(NewtonsoftJsonBodyModelBinder);
                }
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            //Use default MvcNewtonsoftJsonOptions injected globally
            var jsonOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();
            var jsonSettings = jsonOptions?.Value.SerializerSettings??new JsonSerializerSettings();
            if (context.Controller.GetType().FullName?.StartsWith("Elsa.Server.Api")==true)
            {
                //If controller's namespace start with Elsa.Server.Api.Endpoints.WorkflowDefinitions, use GetSettingsForWorkflowDefinition get get SerializerSettings
                if (context.Controller.GetType().FullName?.StartsWith("Elsa.Server.Api.Endpoints.WorkflowDefinitions")==true)
                {
                    jsonSettings = SerializationHelper.GetSettingsForWorkflowDefinition();
                }
                //If controller's namespace start with Elsa.Server.Api, use GetSettingsForEndpoint get get SerializerSettings
                else
                {
                    jsonSettings = SerializationHelper.GetSettingsForEndpoint();
                }
            }
            if (context.Result is ObjectResult objectResult)
            {

                objectResult.Formatters.RemoveType<SystemTextJsonOutputFormatter>();
                objectResult.Formatters.Add(new NewtonsoftJsonOutputFormatter(
                    jsonSettings,
                    context.HttpContext.RequestServices.GetRequiredService<ArrayPool<char>>(),
                    context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value));

            }
            //For JsonResult, there is no way to change the formatter, so change them to Object result
            else if (context.Result is JsonResult jr)
            {
                var obj = new ObjectResult(jr.Value);

                obj.Formatters.RemoveType<SystemTextJsonOutputFormatter>();
                obj.Formatters.Add(new NewtonsoftJsonOutputFormatter(
                    jsonSettings,
                    context.HttpContext.RequestServices.GetRequiredService<ArrayPool<char>>(),
                    context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value));

                context.Result = obj;
            }
            else
            {
                base.OnActionExecuted(context);
            }
        }
    }

    public class NewtonsoftJsonBodyModelBinder : BodyModelBinder
    {
        public NewtonsoftJsonBodyModelBinder(
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
            return new IInputFormatter[]
            {
            new NewtonsoftJsonInputFormatter(
                loggerFactory.CreateLogger<NewtonsoftJsonBodyModelBinder>(),
                jsonOptionsValue.SerializerSettings,
                charPool,
                objectPoolProvider,
                mvcOptions.Value,
                jsonOptionsValue)
            };
        }
    }
}
