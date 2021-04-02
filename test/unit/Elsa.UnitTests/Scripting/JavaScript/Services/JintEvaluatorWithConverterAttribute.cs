using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;
using MediatR;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.JavaScript.Services
{
    /// <summary>
    /// Configures an instance of <see cref="JintJavaScriptEvaluator"/> and adds in the real implementations of
    /// its result converters using <see cref="JintEvaluationResultConverterFactory"/>.
    /// </summary>
    public class JintEvaluatorWithConverterAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new JintEvaluatorWithConverterCustomization(parameter);

        class JintEvaluatorWithConverterCustomization : SpecimenBuilderForParameterCustomization
        {
            public override void Customize(IFixture fixture)
            {
                new ServiceProviderForConvertersAttribute.ServiceProviderForConvertersCustomization().Customize(fixture);
                base.Customize(fixture);
            }

            protected override ISpecimenBuilder GetUnfilteredSpecimenBuilder()
                => new JintEvaluatorWithConverterSpecimenBuilder();

            public JintEvaluatorWithConverterCustomization(ParameterInfo parameter) : base(parameter) {}
        }

        class JintEvaluatorWithConverterSpecimenBuilder : ISpecimenBuilder
        {
            public object Create(object request, ISpecimenContext context)
            {
                if(!request.IsAnAutofixtureRequestForType<JintJavaScriptEvaluator>())
                    return new NoSpecimen();

                var converter = new JintEvaluationResultConverterFactory(context.Resolve<IServiceProvider>())
                    .GetConverter();

                return new JintJavaScriptEvaluator(context.Resolve<IMediator>(),
                                                   context.Resolve<IOptions<ScriptOptions>>(),
                                                   converter);
            }
        }
    }
}