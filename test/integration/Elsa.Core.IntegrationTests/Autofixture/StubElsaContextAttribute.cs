using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class StubElsaContextAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new StubElsaContextCustomization(parameter);
    }
}