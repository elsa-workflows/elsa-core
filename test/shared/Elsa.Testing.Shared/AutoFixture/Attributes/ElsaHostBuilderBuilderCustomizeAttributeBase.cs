using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Specifications;
using Elsa.Testing.Shared.Helpers;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    public abstract class ElsaHostBuilderBuilderCustomizeAttributeBase : CustomizeAttribute
    {
        public abstract Action<ElsaHostBuilderBuilder> GetBuilderCustomizer();

        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new ElsaHostBuilderBuilderCustomization(GetBuilderCustomizer());

        class ElsaHostBuilderBuilderCustomization : ICustomization
        {
            readonly Action<ElsaHostBuilderBuilder> _builderCustomizer;
            
            public void Customize(IFixture fixture)
            {
                fixture.Behaviors.Add(new ElsaHostBuilderBuilderTransformation(_builderCustomizer));
            }

            public ElsaHostBuilderBuilderCustomization(Action<ElsaHostBuilderBuilder> builderCustomizer)
            {
                _builderCustomizer = builderCustomizer ?? throw new ArgumentNullException(nameof(builderCustomizer));
            }
        }

        class ElsaHostBuilderBuilderTransformation : ISpecimenBuilderTransformation
        {
            readonly Action<ElsaHostBuilderBuilder> _builderCustomizer;
            
            public ISpecimenBuilderNode Transform(ISpecimenBuilder builder)
            {
                return new Postprocessor(builder, new ElsaHostBuilderBuilderCommand(_builderCustomizer), IsInstanceOf.Type<ElsaHostBuilderBuilder>());
            }

            public ElsaHostBuilderBuilderTransformation(Action<ElsaHostBuilderBuilder> builderCustomizer)
            {
                _builderCustomizer = builderCustomizer ?? throw new ArgumentNullException(nameof(builderCustomizer));
            }
        }

        class ElsaHostBuilderBuilderCommand : ISpecimenCommand
        {
            readonly Action<ElsaHostBuilderBuilder> _builderCustomizer;
            
            public void Execute(object specimen, ISpecimenContext context)
            {
                var builder = (ElsaHostBuilderBuilder) specimen;
                _builderCustomizer(builder);
            }

            public ElsaHostBuilderBuilderCommand(Action<ElsaHostBuilderBuilder> builderCustomizer)
            {
                _builderCustomizer = builderCustomizer ?? throw new ArgumentNullException(nameof(builderCustomizer));
            }
        }
    }
}