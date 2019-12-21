using System;
using AutoFixture;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.Autofixture.Abstractions;
using NodaTime;

namespace Elsa.Testing.Shared.Autofixture.SpecimenBuilders
{
    public class InstantGenerator : SpecimenBuilderBase<Instant>
    {
        protected override object CreateSpecimen(object request, ISpecimenContext context)
        {
            var dateTime = context.Create<DateTime>();
            return Instant.FromDateTimeUtc(new DateTime(dateTime.Ticks, DateTimeKind.Utc));
        }
    }
}