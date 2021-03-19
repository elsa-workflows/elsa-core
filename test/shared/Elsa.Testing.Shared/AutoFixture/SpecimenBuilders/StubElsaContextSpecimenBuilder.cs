using AutoFixture.Kernel;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class StubElsaContextSpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
            => request.IsAnAutofixtureRequestForType<ElsaContext>()? GetContext(context) : new NoSpecimen();

        static object GetContext(ISpecimenContext context)
        {
            var options = (DbContextOptions<ElsaContext>) context.Resolve(typeof(DbContextOptions<ElsaContext>));
            return new ElsaContext(options);
        }
    }
}