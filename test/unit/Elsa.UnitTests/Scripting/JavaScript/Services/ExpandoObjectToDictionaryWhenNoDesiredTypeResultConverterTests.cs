using System.Collections.Generic;
using Elsa.Testing.Shared;
using Jint;
using Xunit;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverterTests
    {
        [Theory(DisplayName = "The ConvertToDesiredType method should be able to convert a JS object into a dictionary of strings/objects when the desired type is object."), AutoMoqData]
        public void ConvertToDesiredTypeCanConvertJSObjectToDictionaryWhenDesiredTypeIsObject(ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter sut)
        {
            var engine = new Engine();
            engine.Execute(@"JSON.parse('{ ""Foo"": 20, ""Bar"": ""A string"" }')");
            var result = engine.GetCompletionValue().ToObject();

            var expected = new Dictionary<string,object> { { "Foo", 20 } , { "Bar", "A string" } };
            var actual = sut.ConvertToDesiredType(result, typeof(object));

            Assert.IsAssignableFrom<Dictionary<string,object>>(actual);
            var actualDictionary = (Dictionary<string,object>) actual;
            Assert.Equal(20d, actualDictionary["Foo"]);
            Assert.Equal("A string", actualDictionary["Bar"]);
        }
    }
}