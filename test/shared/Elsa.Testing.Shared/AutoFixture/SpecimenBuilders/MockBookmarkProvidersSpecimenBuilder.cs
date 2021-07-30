using System.Collections.Generic;
using System.Linq;
using AutoFixture.Kernel;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using Moq;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class MockBookmarkProvidersSpecimenBuilder : ISpecimenBuilder
    {
        readonly int howMany;

        public object Create(object request, ISpecimenContext context)
        {
            if (!request.IsAnAutofixtureRequestForType<IEnumerable<IBookmarkProvider>>())
                return new NoSpecimen();

            return Enumerable.Range(0, howMany)
                .Select(x => Mock.Of<IBookmarkProvider>())
                .ToList();
        }

        public MockBookmarkProvidersSpecimenBuilder(int howMany)
        {
            this.howMany = howMany;
        }
    }
}