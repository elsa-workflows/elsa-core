using System;
using AutoFixture;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.Abstractions;
using NodaTime;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class PeriodGenerator : SpecimenBuilderBase<Period>
    {
        protected override object CreateSpecimen(object request, ISpecimenContext context) =>
            Period.FromTicks(context.Create<TimeSpan>().Ticks);
    }
}