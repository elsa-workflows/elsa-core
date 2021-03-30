using System;
using Elsa.Activities.Http.Models;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Elsa.Activities.Http
{
    public class StringValuesModelTests
    {
        [Theory(DisplayName = "It should be possible to use Convert.ChangeType to convert an instance to string.  This is primarily to support usage via Jint."), AutoMoqData]
        public void ItShouldBePossibleToConvertAStringValuesModelToString(string[] values)
        {
            var sut = new StringValuesModel(new StringValues(values));
            var result = Convert.ChangeType(sut, typeof(string));
            Assert.Equal(sut.ToString(), result);
        }

        [Fact(DisplayName = "It should be possible to use Convert.ToInt32 to convert an instance to a number if the value is a valid string representation of a number.  This is primarily to support usage via Jint.")]
        public void ItShouldBePossibleToConvertAStringValuesModelToInt32IfValid()
        {
            var sut = new StringValuesModel(new StringValues("3"));
            var result = Convert.ChangeType(sut, typeof(int));
            Assert.Equal(3, result);
        }

        [Theory(DisplayName = "The Value property should return the first value if there is more than one."), AutoMoqData]
        public void ValueShouldReturnFirstItemIfThereAreMoreThanOne(string firstValue, string secondValue)
        {
            var sut = new StringValuesModel(new StringValues(new [] { firstValue, secondValue }));
            Assert.Equal(firstValue, sut.Value);
        }

        [Theory(DisplayName = "The Value property should always return the first value if there is only one."), AutoMoqData]
        public void ValueShouldReturnFirstItemIfThereIsOne(string firstValue)
        {
            var sut = new StringValuesModel(new StringValues(new [] { firstValue }));
            Assert.Equal(firstValue, sut.Value);
        }

        [Fact(DisplayName = "The Value property should return null if there are no values.")]
        public void ValueShouldReturnNullIfThereAreNone()
        {
            var sut = new StringValuesModel(new StringValues(new string[0]));
            Assert.Null(sut.Value);
        }

        [Theory(DisplayName = "The Values property should return a length-one collection if there is a single value."), AutoMoqData]
        public void ValuesShouldReturnLengthOneCollectionIfThereIsOneValue(string firstValue)
        {
            var sut = new StringValuesModel(new StringValues(new [] { firstValue }));
            Assert.Equal(new [] {firstValue}, sut.Values);
        }

        [Theory(DisplayName = "The Values property should return a length-two collection if there are two values."), AutoMoqData]
        public void ValuesShouldReturnLengthTwoCollectionIfThereAreTwoValues(string firstValue, string secondValue)
        {
            var sut = new StringValuesModel(new StringValues(new [] { firstValue, secondValue }));
            Assert.Equal(new [] {firstValue, secondValue}, sut.Values);
        }

        [Fact(DisplayName = "The Values property should return an empty collection if there are no values.")]
        public void ValuesShouldReturnEmptyCollectionIfThereAreNone()
        {
            var sut = new StringValuesModel(new StringValues(new string[0]));
            Assert.Empty(sut.Values);
        }

        [Fact(DisplayName = "The ToString method should return null if there are no values")]
        public void ToStringShouldReturnNullIfNotValues()
        {
            var sut = new StringValuesModel(new StringValues(new string[0]));
            Assert.Null(sut.ToString());
        }

        [Theory(DisplayName = "The ToString method should return the first value if there is only one"), AutoMoqData]
        public void ToStringShouldReturnFirstValueIfThereIsOnlyOne(string firstValue)
        {
            var sut = new StringValuesModel(new StringValues(new [] { firstValue }));
            Assert.Equal(firstValue, sut.ToString());
        }

        [Theory(DisplayName = "The ToString method should return comma-separated values if there are more than one"), AutoMoqData]
        public void ToStringShouldReturnCommaSeparatedValuesIfThereIsMoreThanOne(string firstValue, string secondValue)
        {
            var sut = new StringValuesModel(new StringValues(new [] { firstValue, secondValue }));
            Assert.Equal($"{firstValue},{secondValue}", sut.ToString());
        }

        [Fact(DisplayName = "The parameterless constructor should initialize Values to an empty collection")]
        public void ParameterlessCtorShouldInitializeWithEmptyCollection()
        {
            var sut = new StringValuesModel();
            Assert.Empty(sut.Values);
        }
    }
}