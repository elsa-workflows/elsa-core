using System;
using AutoFixture;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.Abstractions;
using NodaTime;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class DurationGenerator : SpecimenBuilderBase<Duration>
    {
        protected override object CreateSpecimen(object request, ISpecimenContext context) =>
            Duration.FromTimeSpan(context.Create<TimeSpan>());
    }
}