using System.Collections.Generic;
using Elsa.Testing.Shared;
using Jint;
using Xunit;

namespace Elsa.Scripting.JavaScript.Services
{
    public class EnumerableResultConverterTests
    {
        [Theory(DisplayName = "The ConvertToDesiredType method should be able to convert a JS array of string/number objects to a List of single-item string/double dictionaries."), AutoMoqData]
        public void ConvertToDesiredTypeCanConvertArrayOfStringAndNumberObjectsToAListOfDictionaries(EnumerableResultConverter sut)
        {
            var engine = new Engine();
            var tuples = engine.Evaluate(@"[{""foo"":5},{""bar"":15}]").ToObject();

            var expected = new List<Dictionary<string,double>> {
                new () { { "foo", 5 } },
                new () { { "bar", 15 } },
            };
            Assert.Equal(expected, sut.ConvertToDesiredType(tuples, typeof(List<Dictionary<string,double>>)));
        }

        [Theory(DisplayName = "The ConvertToDesiredType method should be able to convert a JS array of strings to a List of strings."), AutoMoqData]
        public void ConvertToDesiredTypeCanConvertArrayOfStringsToAListOfStrings(EnumerableResultConverter sut)
        {
            var engine = new Engine();
            var tuples = engine.Evaluate(@"[""foo"",""bar""]").ToObject();

            var expected = new List<string> { "foo", "bar" };
            Assert.Equal(expected, sut.ConvertToDesiredType(tuples, typeof(List<string>)));
        }

        [Theory(DisplayName = "The ConvertToDesiredType method should convert a JS array of strings to an array of strings if the desired type is object."), AutoMoqData]
        public void ConvertToDesiredTypeShouldConvertArrayOfStringsToArrayOfStringsWhenNoDesiredTypeIsSpecified(EnumerableResultConverter sut)
        {

            var engine = new Engine();
            var tuples = engine.Evaluate(@"[""foo"",""bar""]").ToObject();

            var expected = new [] { "foo", "bar" };
            Assert.Equal(expected, sut.ConvertToDesiredType(tuples, typeof(object)));
        }

        // Known issue:
        // We would like to create a test for the following requirement, but so far this has proven impossible to implement.
        // It might be that an upstream improvement to Jint could help us, but as things stand this will not actually work.
        // Instead of single-item dictionaries, we get an array of JToken.  JTokens cause a StackOverflowException if an attempt
        // is made to use them with JSON.stringify.  See https://github.com/elsa-workflows/elsa-core/issues/761
        // 
        // The ConvertToDesiredType method should convert a JS array of string/number objects to an array of single-item string/double dictionaries if the desired type is object.
    }
}