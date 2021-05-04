using System;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.Abstractions;
using NodaTime;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class LocalDateGenerator : SpecimenBuilderBase<LocalDate>
    {
        protected override object CreateSpecimen(object request, ISpecimenContext context) =>
            LocalDate.FromDateTime(DateTime.Today);
    }
}