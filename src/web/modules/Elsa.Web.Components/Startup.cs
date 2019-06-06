using Elsa.Core.Extensions;
using Elsa.Web.Components.Metadata;
using Elsa.Web.Components.Services;
using Elsa.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Elsa.Web.Components
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWorkflowsCore()
                .AddWorkflowsDesigner()
                .AddScoped<IActivityDisplayManager, ActivityDisplayManager>()
                .AddScoped<IActivityShapeFactory, ActivityShapeFactory>();

            services.AddMvc(options =>
            {
                options.ModelMetadataDetailsProviders.Add(new OptionsMetadataProvider());
                options.ModelMetadataDetailsProviders.Add(new WorkflowExpressionDataTypeMetadataProvider());
            });
        }
    }
}
