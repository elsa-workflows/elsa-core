using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    public class MockBookmarkProvidersAttribute : CustomizeAttribute
    {
        readonly int _howMany;

        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new MockBookmarkProvidersCustomization(parameter, _howMany);

        public MockBookmarkProvidersAttribute(int howMany = 3)
        {
            _howMany = howMany;
        }
    }
}