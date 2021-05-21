using System.Buffers;
using System.Linq;
using Elsa.Serialization;
using Elsa.Server.Api.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Server.Api.ActionFilters
{
    public class ElsaJsonFormatterAttribute : ActionFilterAttribute, IControllerModelConvention, IActionModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
                Apply(action);
        }

        public void Apply(ActionModel action)
        {
            // Set the model binder to NewtonsoftJsonBodyModelBinder for parameters that are bound to the request body.
            var parameters = action.Parameters.Where(p => p.BindingInfo?.BindingSource == BindingSource.Body);

            foreach (var p in parameters)
                p.BindingInfo.BinderType = typeof(ElsaJsonBodyModelBinder);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var services = context.HttpContext.RequestServices;
                var serializerSettings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();

                objectResult.Formatters.RemoveType<SystemTextJsonOutputFormatter>();
                objectResult.Formatters.Add(new NewtonsoftJsonOutputFormatter(
                    serializerSettings,
                    services.GetRequiredService<ArrayPool<char>>(),
                    services.GetRequiredService<IOptions<MvcOptions>>().Value));
            }
            else
            {
                base.OnActionExecuted(context);
            }
        }
    }
}