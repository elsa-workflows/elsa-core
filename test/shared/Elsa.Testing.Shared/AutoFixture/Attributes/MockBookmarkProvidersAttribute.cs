using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    public class MockBookmarkProvidersAttribute : CustomizeAttribute
    {
        readonly int howMany;

        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new MockBookmarkProvidersCustomization(parameter, howMany);

        public MockBookmarkProvidersAttribute(int howMany = 3)
        {
            this.howMany = howMany;
        }
    }
}