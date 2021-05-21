using System;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Xunit;

namespace Elsa.Expressions
{
    public class LiteralHandlerTests
    {
        [Theory(DisplayName = "When the desired return type is a string, the EvaluateAsync method should return the literal expression"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnExpressionLiteralWhenReturnTypeIsString(LiteralHandler sut,
                                                                                           string expression,
                                                                                           [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            var result = await sut.EvaluateAsync(expression, typeof(string), context, default);
            Assert.Equal(expression, result);
        }

        [Theory(DisplayName = "When the desired return type is an object, the EvaluateAsync method should return the literal expression"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnExpressionLiteralWhenReturnTypeIsObject(LiteralHandler sut,
                                                                                           string expression,
                                                                                           [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            var result = await sut.EvaluateAsync(expression, typeof(object), context, default);
            Assert.Equal(expression, result);
        }

        [Theory(DisplayName = "When the desired return type is not string or object but the expression is null, the EvaluateAsync method should return null"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnNullWhenExpressionIsNull(LiteralHandler sut,
                                                                                           [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            var result = await sut.EvaluateAsync(null, typeof(Uri), context, default);
            Assert.Null(result);
        }

        [Theory(DisplayName = "When the desired return type is not string or object but the expression is empty string, the EvaluateAsync method should return null"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnNullWhenExpressionIsEmpty(LiteralHandler sut,
                                                                             [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            var result = await sut.EvaluateAsync(string.Empty, typeof(Uri), context, default);
            Assert.Null(result);
        }

        [Theory(DisplayName = "When the desired return type is not string or object but the expression is whitespace-only, the EvaluateAsync method should return null"), AutoMoqData]
        public async Task EvaluateAsyncShouldReturnNullWhenExpressionIsWhitespaceOnly(LiteralHandler sut,
                                                                                      [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            var result = await sut.EvaluateAsync("  ", typeof(Uri), context, default);
            Assert.Null(result);
        }

        [Theory(DisplayName = "When the desired return type is DateTime, the EvaluateAsync method should convert the string to a DateTime"), AutoMoqData]
        public async Task EvaluateAsyncShouldConvertToDateTimeWhenThatIsDesiredReturnType(LiteralHandler sut,
                                                                                          [StubActivityExecutionContext] ActivityExecutionContext context)
        {
            var result = await sut.EvaluateAsync("2001-03-04T12:45:32Z", typeof(DateTime), context, default);
            var expected = new DateTime(2001, 03, 04, 12, 45, 32, DateTimeKind.Utc);
            Assert.Equal(expected, result);
        }
    }
}