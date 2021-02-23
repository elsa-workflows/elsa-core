using System.Linq;
using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    public class OmitOnRecursionAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new OmitOnRecursionCustomization();
     
        class OmitOnRecursionCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                var throwingBehaviours = fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList();
                foreach(var behaviour in throwingBehaviours)
                    fixture.Behaviors.Remove(behaviour);

                fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            }
        }
    }
}