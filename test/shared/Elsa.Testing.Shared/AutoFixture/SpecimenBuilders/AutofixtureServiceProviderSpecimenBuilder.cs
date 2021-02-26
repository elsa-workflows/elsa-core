using System;
using System.Reflection;
using AutoFixture.Kernel;
using Moq;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    /// <summary>
    /// Creates a mock <see cref="IServiceProvider"/> which gets services
    /// by resolving them from Autofixture.
    /// </summary>
    public class AutofixtureServiceProviderSpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if(Equals(request, typeof(IServiceProvider)))
                return GetServiceProvider(context);
            if(request is ParameterInfo paramInfo && paramInfo.ParameterType == typeof(IServiceProvider))
                return GetServiceProvider(context);

            return new NoSpecimen();
        }

        static object GetServiceProvider(ISpecimenContext context)
        {
            var provider = new Mock<IServiceProvider>();

            provider.Name = $"Autofixture_{nameof(IServiceProvider)}-{Guid.NewGuid()}";
            provider
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns((Type t) => context.Resolve(t));

            return provider.Object;
        }
    }
}