using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Fluid;
using Moq;
using Xunit;

namespace Elsa.UnitTests.Expressions
{
    public class LiquidHandlerTests
    {
        [Theory(DisplayName = "When the desired return type is a string, the EvaluateAsync method should return the expression result"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnExpressionResultWhenReturnTypeIsString([Frozen] ILiquidTemplateManager liquidTemplateManager,
                                                                                          LiquidHandler sut,
                                                                                          string expression,
                                                                                          string expectedResult,
                                                                                          [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            Mock.Get(liquidTemplateManager)
                .Setup(x => x.RenderAsync(expression, It.IsAny<TemplateContext>(), HtmlEncoder.Default))
                .Returns(() => Task.FromResult(expectedResult));

            var result = await sut.EvaluateAsync(expression, typeof(string), context, default);

            Assert.Equal(expectedResult, result);
        }

        [Theory(DisplayName = "When the desired return type is an object, the EvaluateAsync method should return the expression result"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnExpressionResultWhenReturnTypeIsObject([Frozen] ILiquidTemplateManager liquidTemplateManager,
                                                                                          LiquidHandler sut,
                                                                                          string expression,
                                                                                          string expectedResult,
                                                                                          [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            Mock.Get(liquidTemplateManager)
                .Setup(x => x.RenderAsync(expression, It.IsAny<TemplateContext>(), HtmlEncoder.Default))
                .Returns(() => Task.FromResult(expectedResult));

            var result = await sut.EvaluateAsync(expression, typeof(object), context, default);

            Assert.Equal(expectedResult, result);
        }

        [Theory(DisplayName = "When the expression result is null, the EvaluateAsync method should return null"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnNullWhenExpressionIsNull([Frozen] ILiquidTemplateManager liquidTemplateManager,
                                                                            LiquidHandler sut,
                                                                            string expression,
                                                                            [StubActivityExecutionContext] ActivityExecutionContext context,
                                                                            Type returnType)
        {
            Mock.Get(liquidTemplateManager)
                .Setup(x => x.RenderAsync(expression, It.IsAny<TemplateContext>(), HtmlEncoder.Default))
                .Returns(() => Task.FromResult<string>(null));

            var result = await sut.EvaluateAsync(expression, returnType, context, default);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When the expression result is empty string, the EvaluateAsync method should return null"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnNullWhenExpressionIsEmpty([Frozen] ILiquidTemplateManager liquidTemplateManager,
                                                                             LiquidHandler sut,
                                                                             string expression,
                                                                             [StubActivityExecutionContext] ActivityExecutionContext context,
                                                                             Type returnType)
        {
            Mock.Get(liquidTemplateManager)
                .Setup(x => x.RenderAsync(expression, It.IsAny<TemplateContext>(), HtmlEncoder.Default))
                .Returns(() => Task.FromResult(String.Empty));

            var result = await sut.EvaluateAsync(expression, returnType, context, default);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When the expression result is whitespace-only, the EvaluateAsync method should return null"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnNullWhenExpressionIsWhitespaceOnly([Frozen] ILiquidTemplateManager liquidTemplateManager,
                                                                                      LiquidHandler sut,
                                                                                      string expression,
                                                                                      [StubActivityExecutionContext] ActivityExecutionContext context,
                                                                                      Type returnType)
        {
            Mock.Get(liquidTemplateManager)
                .Setup(x => x.RenderAsync(expression, It.IsAny<TemplateContext>(), HtmlEncoder.Default))
                .Returns(() => Task.FromResult("  "));

            var result = await sut.EvaluateAsync(expression, returnType, context, default);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When the desired return type is DateTime, the EvaluateAsync method should convert the result to a DateTime"), AutoMoqData]
        public async Task EvaluateAsyncShouldConvertToDateTimeWhenThatIsDesiredReturnType([Frozen] ILiquidTemplateManager liquidTemplateManager,
                                                                                          LiquidHandler sut,
                                                                                          string expression,
                                                                                          [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            Mock.Get(liquidTemplateManager)
                .Setup(x => x.RenderAsync(expression, It.IsAny<TemplateContext>(), HtmlEncoder.Default))
                .Returns(() => Task.FromResult("2001-03-04T12:45:32Z"));

            var result = await sut.EvaluateAsync(expression, typeof(DateTime), context, default);

            Assert.Equal(new DateTime(2001, 03, 04, 12, 45, 32, DateTimeKind.Utc), result);
        }
    }
}