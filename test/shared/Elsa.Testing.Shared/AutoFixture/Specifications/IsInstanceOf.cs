using System;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Specifications
{
    public class IsInstanceOf : IRequestSpecification
    {
        readonly Type _type;

        public bool IsSatisfiedBy(object request) => request.IsAnAutofixtureRequestForType(_type);

        public IsInstanceOf(Type type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public static IsInstanceOf Type<T>() => new IsInstanceOf(typeof(T));
    }
}