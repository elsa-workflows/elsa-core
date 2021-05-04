using System;
using AutoFixture.Kernel;

namespace Elsa.Testing.Shared.AutoFixture.Abstractions
{
    public abstract class SpecimenBuilderBase<T> : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return !typeof(T).Equals(request) ? new NoSpecimen() : CreateSpecimen(request, context);
        }

        protected abstract object CreateSpecimen(object request, ISpecimenContext context);
    }
}