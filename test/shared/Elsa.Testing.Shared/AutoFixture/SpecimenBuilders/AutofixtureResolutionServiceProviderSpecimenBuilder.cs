using System;
using AutoFixture.Kernel;
using Moq;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class AutofixtureResolutionServiceProviderSpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if(!request.IsAnAutofixtureRequestForType<IServiceProvider>())
                return new NoSpecimen();

            var provider = Mock.Of<IServiceProvider>();
            AddAutofixtureResolution(provider, context);
            return provider;
        }

        public static void AddAutofixtureResolution(IServiceProvider provider, ISpecimenContext context)
        {
            var mock = Mock.Get(provider);

            mock.Name = $"Autofixture_{nameof(IServiceProvider)}-{Guid.NewGuid()}";
            mock
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns((Type t) => context.Resolve(t));

        }
    }
}