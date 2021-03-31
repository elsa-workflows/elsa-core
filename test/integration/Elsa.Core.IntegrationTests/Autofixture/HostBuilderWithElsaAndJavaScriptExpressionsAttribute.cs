using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithElsaAndJavaScriptExpressionsAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBuilderUsingServicesCustomization(services => {
                services.AddElsa().AddJavaScriptExpressionEvaluator();
            }, parameter);
        }
    }
}