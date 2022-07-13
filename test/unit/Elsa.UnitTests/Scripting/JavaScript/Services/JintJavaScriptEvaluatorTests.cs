using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using MediatR;
using Moq;
using Xunit;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JintJavaScriptEvaluatorTests
    {
        /* Strictly-speaking these are integration tests.  They cover the JintJavaScriptEvaluator and also all of
         * the types which implement IConvertsJintEvaluationResult. Historically, all of this functionality used to be
         * in one class.
         */

        [Theory(DisplayName = "The EvaluateAsync method should be able to roundtrip a JSON object via JSON.parse and then JSON.stringify"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToRoundtripAJsonStringViaJsonStringify(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1,
            [StubActivityExecutionContext] ActivityExecutionContext context2)
        {
            object returnedValue = null;
            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context2), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = await sut.EvaluateAsync(@"JSON.parse(""{\""foo\"":\""bar\""}"")", typeof(object), context1);
            var result = await sut.EvaluateAsync("JSON.stringify(MyVariable)", typeof(object), context2);

            Assert.Equal(@"{""foo"":""bar""}", result);
        }

        [Theory(DisplayName = "The EvaluateAsync method should be able to roundtrip a JSON object (which contains an embedded object) via JSON.parse and then JSON.stringify"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToRoundtripAJsonStringWithAnEmbeddedObjectViaJsonStringify(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1,
            [StubActivityExecutionContext] ActivityExecutionContext context2)
        {
            object returnedValue = null;
            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context2), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = await sut.EvaluateAsync(@"JSON.parse(""{\""foo\"":\""bar\"", \""child\"":{\""one\"":\""two\""}}"")", typeof(object), context1);
            var result = await sut.EvaluateAsync("JSON.stringify(MyVariable)", typeof(object), context2);

            Assert.Equal(@"{""foo"":""bar"",""child"":{""one"":""two""}}", result);
        }

        [Theory(DisplayName = "The EvaluateAsync method should be able to roundtrip a JSON object (which contains an embedded array) via JSON.parse and then JSON.stringify"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToRoundtripAJsonStringWithAnEmbeddedArrayViaJsonStringify(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1,
            [StubActivityExecutionContext] ActivityExecutionContext context2)
        {
            object returnedValue = null;
            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context2), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = await sut.EvaluateAsync(@"JSON.parse(""{\""foo\"":\""bar\"", \""child\"":[\""one\"",\""two\""]}"")", typeof(object), context1);
            var result = await sut.EvaluateAsync("JSON.stringify(MyVariable)", typeof(object), context2);

            Assert.Equal(@"{""foo"":""bar"",""child"":[""one"",""two""]}", result);
        }

        [Theory(DisplayName = "The EvaluateAsync method should be able to roundtrip a JSON array via JSON.parse and then JSON.stringify"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToRoundtripAJsonStringOfAnArrayViaJsonStringify(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1,
            [StubActivityExecutionContext] ActivityExecutionContext context2)
        {
            object returnedValue = null;
            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context2), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = await sut.EvaluateAsync(@"JSON.parse(""[\""foo\"",\""bar\""]"")", typeof(object), context1);
            var result = await sut.EvaluateAsync("JSON.stringify(MyVariable)", typeof(object), context2);

            Assert.Equal(@"[""foo"",""bar""]", result);
        }

        [Theory(DisplayName = "The EvaluateAsync method should be able to access a property of an object which was created via JSON.parse"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToAccessAPropertyWhichWasParsed(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1,
            [StubActivityExecutionContext] ActivityExecutionContext context2)
        {
            object returnedValue = null;
            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context2), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = await sut.EvaluateAsync(@"JSON.parse(""{\""foo\"":\""bar\""}"")", typeof(object), context1);
            var result = await sut.EvaluateAsync("MyVariable.foo", typeof(object), context2);

            Assert.Equal("bar", result);
        }

        [Theory(DisplayName = "The EvaluateAsync method should be able to evaluate and return a NodaTime duration"), AutoMoqData]
        public async Task EvaluateAsyncShouldBeAbleToEvaluateAndReturnANodaTimeObject(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1)
        {
            var duration1 = NodaTime.Duration.FromDays(1);
            var duration2 = NodaTime.Duration.FromHours(12);

            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context1), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) =>
                {
                    expression.Engine.SetValue("duration1", duration1);
                    expression.Engine.SetValue("duration2", duration2);
                });

            var result = await sut.EvaluateAsync("duration1.Plus(duration2)", typeof(NodaTime.Duration), context1);

            Assert.Equal(NodaTime.Duration.FromHours(36), result);
        }
    }
}