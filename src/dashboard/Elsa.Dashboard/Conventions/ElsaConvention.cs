using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Elsa.Dashboard.Conventions
{
    public abstract class ElsaConvention : IControllerModelConvention
    {
        public const string AreaName = "Elsa";

        public void Apply(ControllerModel controller)
        {
            if (controller.RouteValues["area"] == AreaName)
            {
                ApplyConvention(controller);
            }
        }

        protected abstract void ApplyConvention(ControllerModel controller);
    }
}