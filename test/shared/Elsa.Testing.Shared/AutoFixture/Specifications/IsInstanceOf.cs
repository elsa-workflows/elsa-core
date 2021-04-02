using System;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Specifications
{
    public class IsInstanceOf : IRequestSpecification
    {
        readonly Type type;

        public bool IsSatisfiedBy(object request) => request.IsAnAutofixtureRequestForType(type);

        public IsInstanceOf(Type type)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public static IsInstanceOf Type<T>() => new IsInstanceOf(typeof(T));
    }
}