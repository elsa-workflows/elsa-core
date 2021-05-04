using AutoFixture;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture
{
    public class NodaTimeCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new InstantGenerator());
            fixture.Customizations.Add(new DurationGenerator());
            fixture.Customizations.Add(new PeriodGenerator());
            fixture.Customizations.Add(new LocalDateGenerator());
        }
    }
}