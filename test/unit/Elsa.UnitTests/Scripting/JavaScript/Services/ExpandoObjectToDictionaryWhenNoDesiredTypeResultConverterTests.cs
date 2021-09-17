using System.Collections.Generic;
using Elsa.Testing.Shared;
using Jint;
using Xunit;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverterTests
    {
        [Theory(DisplayName = "The ConvertToDesiredType method should be able to convert a JS object into a dictionary of strings/objects when the desired type is object."), AutoMoqData]
        public void ConvertToDesiredTypeCanConvertJsObjectToDictionaryWhenDesiredTypeIsObject(ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter sut)
        {
            var engine = new Engine();
            var result = engine.Evaluate(@"JSON.parse('{ ""Foo"": 20, ""Bar"": ""A string"" }')").ToObject();
            var actual = sut.ConvertToDesiredType(result, typeof(object));

            Assert.IsAssignableFrom<Dictionary<string,object>>(actual);
            var actualDictionary = (Dictionary<string,object>) actual;
            Assert.Equal(20d, actualDictionary["Foo"]);
            Assert.Equal("A string", actualDictionary["Bar"]);
        }
    }
}