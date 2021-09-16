using System.Collections.Generic;
using AutoFixture.Kernel;
using Elsa.Services.Models;
using Moq;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class StubActivityBlueprintSpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
            => request.IsAnAutofixtureRequestForType<IActivityBlueprint>()? GetContext() : new NoSpecimen();

        static object GetContext()
        {
            var mock = new Mock<IActivityBlueprint>();

            mock.Setup(x => x.PropertyStorageProviders).Returns(new Dictionary<string, string>());

            return mock.Object;
        }
    }
}