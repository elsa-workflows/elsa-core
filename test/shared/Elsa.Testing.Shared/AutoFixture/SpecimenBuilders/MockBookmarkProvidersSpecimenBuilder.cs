using System.Collections.Generic;
using System.Linq;
using AutoFixture.Kernel;
using Elsa.Services;
using Moq;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class MockBookmarkProvidersSpecimenBuilder : ISpecimenBuilder
    {
        readonly int _howMany;

        public object Create(object request, ISpecimenContext context)
        {
            if (!request.IsAnAutofixtureRequestForType<IEnumerable<IBookmarkProvider>>())
                return new NoSpecimen();

            return Enumerable.Range(0, _howMany)
                .Select(x => Mock.Of<IBookmarkProvider>())
                .ToList();
        }

        public MockBookmarkProvidersSpecimenBuilder(int howMany)
        {
            _howMany = howMany;
        }
    }
}