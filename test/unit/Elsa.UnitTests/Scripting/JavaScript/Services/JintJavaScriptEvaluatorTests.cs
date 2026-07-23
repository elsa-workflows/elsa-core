using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Scripting.JavaScript.Converters.Jint;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Jint;
using Jint.Native;
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

        [Theory(DisplayName = "ByteArrayConverter: The EvaluateAsync method should expose a byte array as an ArrayBuffer and preserve its values"), AutoMoqData]
        public async Task EvaluateAsync_ShouldExposeByteArrayAsArrayBufferWithCorrectValues(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1)
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            object returnedValue = null;

            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context1), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = bytes;

            // Check length
            var lengthResult = await sut.EvaluateAsync("MyVariable.byteLength", typeof(object), context1);
            Assert.Equal((double)bytes.Length, lengthResult);

            // Check values
            var jsArray = await sut.EvaluateAsync("Array.from(new Uint8Array(MyVariable))", typeof(object), context1);
            var resultArray = ((IEnumerable<object>)jsArray).Select(Convert.ToByte).ToArray();
            Assert.Equal(bytes, resultArray);
        }

        [Fact(DisplayName = "ByteArrayConverter: Should return false and JsValue.Null for null input")]
        public void TryConvert_ShouldReturnFalse_ForNull()
        {
            var converter = new ByteArrayConverter();
            var engine = new Engine();
            var result = JsValue.Undefined;

            var success = converter.TryConvert(engine, null, out result);

            Assert.False(success);
            Assert.Equal(JsValue.Null, result);
        }

        [Fact(DisplayName = "ByteArrayConverter: Should return false for non-byte array input")]
        public void TryConvert_ShouldReturnFalse_ForNonByteArray()
        {
            var converter = new ByteArrayConverter();
            var engine = new Engine();
            var result = JsValue.Undefined;

            var success = converter.TryConvert(engine, new int[] { 1, 2, 3 }, out result);

            Assert.False(success);
            Assert.Equal(JsValue.Null, result);
        }

        [Theory(DisplayName = "ByteArrayConverter: Should roundtrip a byte array from .NET to JS and back"), AutoMoqData]
        public async Task EvaluateAsync_ShouldRoundtripByteArray(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1)
        {
            var bytes = new byte[] { 10, 20, 30, 40 };
            object returnedValue = null;

            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context1), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });

            returnedValue = bytes;
            var jsArray = await sut.EvaluateAsync("Array.from(new Uint8Array(MyVariable))", typeof(object), context1);

            var resultArray = ((IEnumerable<object>)jsArray).Select(x => Convert.ToByte(x)).ToArray();
            Assert.Equal(bytes, resultArray);
        }

        [Theory(DisplayName = "ByteArrayConverter: Should reflect mutation of ArrayBuffer in JS when read back in .NET"), AutoMoqData]
        public async Task EvaluateAsync_ShouldReflectJsMutation(
            [Frozen] IMediator mediator,
            [JintEvaluatorWithConverter] JintJavaScriptEvaluator sut,
            [StubActivityExecutionContext] ActivityExecutionContext context1)
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            object returnedValue = null;

            Mock.Get(mediator)
                .Setup(x => x.Publish(It.Is<EvaluatingJavaScriptExpression>(e => e.ActivityExecutionContext == context1), It.IsAny<CancellationToken>()))
                .Callback((EvaluatingJavaScriptExpression expression, CancellationToken t) => { expression.Engine.SetValue("MyVariable", returnedValue); });
            
            returnedValue = bytes;

            await sut.EvaluateAsync("new Uint8Array(MyVariable)[0] = 99", typeof(object), context1);
            var jsArray = await sut.EvaluateAsync("Array.from(new Uint8Array(MyVariable))", typeof(object), context1);

            var resultArray = ((IEnumerable<object>)jsArray).Select(x => Convert.ToByte(x)).ToArray();
            var expected = new byte[] { 99, 2, 3, 4 };
            Assert.Equal(expected, resultArray);
        }
    }
}