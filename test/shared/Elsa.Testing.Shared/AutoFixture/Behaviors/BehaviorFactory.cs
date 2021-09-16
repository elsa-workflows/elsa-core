using System;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.Specifications;

namespace Elsa.Testing.Shared.AutoFixture.Behaviors
{
    public class BehaviorFactory
    {
        public static ISpecimenBuilderTransformation GetPostprocessingBehavior<T>(Action<T,ISpecimenContext> postProcessingAction) where T : class
            => new PostprocessingTransformation<T>(postProcessingAction);

        public static ISpecimenBuilderTransformation GetPostprocessingBehavior<T>(Action<T> postProcessingAction) where T : class
            => new PostprocessingTransformation<T>((speciman, ctx) => postProcessingAction(speciman));

        class PostprocessingTransformation<T> : ISpecimenBuilderTransformation where T : class
        {
            readonly Action<T,ISpecimenContext> _builderCustomizer;
            
            public ISpecimenBuilderNode Transform(ISpecimenBuilder builder)
            {
                var command = new PostprocessingCommand<T>(_builderCustomizer);
                var spec = IsInstanceOf.Type<T>();
                
                return new Postprocessor(builder, command, spec);
            }

            public PostprocessingTransformation(Action<T,ISpecimenContext> builderCustomizer)
            {
                _builderCustomizer = builderCustomizer ?? throw new ArgumentNullException(nameof(builderCustomizer));
            }
        }

        class PostprocessingCommand<T> : ISpecimenCommand where T : class
        {
            readonly Action<T,ISpecimenContext> _builderCustomizer;
            
            public void Execute(object specimen, ISpecimenContext context)
            {
                var builder = (T) specimen;
                _builderCustomizer(builder, context);
            }

            public PostprocessingCommand(Action<T,ISpecimenContext> builderCustomizer)
            {
                _builderCustomizer = builderCustomizer ?? throw new ArgumentNullException(nameof(builderCustomizer));
            }
        }
    }
}