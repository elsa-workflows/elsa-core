using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Behaviors;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    /// <summary>
    /// Customizes the parameter so that the <see cref="IServiceProvider"/>
    /// created by Moq will resolve services of the appropriate type, creating
    /// them using Autofixture.
    /// </summary>
    /// <seealso cref="AutofixtureServiceProviderSpecimenBuilder"/>
    public class WithAutofixtureResolutionAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new AutofixtureServiceProviderCustomization();

        class AutofixtureServiceProviderCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                var behavior = BehaviorFactory.GetPostprocessingBehavior<IServiceProvider>(AutofixtureResolutionServiceProviderSpecimenBuilder.AddAutofixtureResolution);
                fixture.Behaviors.Add(behavior);
            }
        }

    }
}