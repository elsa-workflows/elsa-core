using System;
using AutoFixture;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.Autofixture.Abstractions;
using NodaTime;

namespace Elsa.Testing.Shared.Autofixture.SpecimenBuilders
{
    public class PeriodGenerator : SpecimenBuilderBase<Period>
    {
        protected override object CreateSpecimen(object request, ISpecimenContext context) =>
            Period.FromTicks(context.Create<TimeSpan>().Ticks);
    }
}