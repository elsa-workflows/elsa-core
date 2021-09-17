using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    public class StubActivityBlueprintAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter) => new StubActivityBlueprintCustomization(parameter);
    }
}