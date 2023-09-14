using Elsa.Server.Api.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Api.Extensions
{
    /// <summary>
    /// Auto apply NewtonsoftJsonFormatterAttribute to all controllers in Elsa.Server.Api
    /// </summary>
    public class ElsaNewtonsoftJsonConvention : IControllerModelConvention
    {

        public ElsaNewtonsoftJsonConvention()
        {
        }

        public void Apply(ControllerModel controller)
        {
            if (ShouldApplyConvention(controller))
            {
                var formatterAttribute = new NewtonsoftJsonFormatterAttribute();

                // The attribute itself also implements IControllerModelConvention so we have to call that one as well.
                // This way, the NewtonsoftJsonBodyModelBinder will be properly connected to the controller actions.
                formatterAttribute.Apply(controller);

                controller.Filters.Add(formatterAttribute);
            }
        }

        private bool ShouldApplyConvention(ControllerModel controller)
        {
            return controller?.ControllerType?.FullName?.StartsWith("Elsa.Server.Api")==true &&
                !controller.Attributes.Any(x => x.GetType() == typeof(NewtonsoftJsonFormatterAttribute));
        }
    }
}
