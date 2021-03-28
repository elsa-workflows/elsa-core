using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Services.Models;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JavaScriptServiceTests
    {
        [Theory(DisplayName = "The EvaluateAsync method should be able to roundtrip a JSON object via JSON.parse and then JSON.stringify"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToRoundtripAJsonStringViaJsonStringify([Frozen] IMediator mediator,
                                                                                          [Frozen] IOptions<ScriptOptions> options,
                                                                                          JavaScriptService sut,
                                                                                          [StubActivityExecutionContext] ActivityExecutionContext context1,
                                                                                          [StubActivityExecutionContext] ActivityExecutionContext context2)
        {
            object returnedValue = null;
            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context2), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => {
                    expression.Engine.SetValue("MyVariable", returnedValue);
                });

            returnedValue = await sut.EvaluateAsync(@"JSON.parse(""{\""foo\"":\""bar\""}"")", typeof(object), context1);
            var result = await sut.EvaluateAsync("JSON.stringify(MyVariable)", typeof(object), context2);

            Assert.Equal(@"{""foo"":""bar""}", result);
        }
    }
}