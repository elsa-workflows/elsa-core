using Elsa.Testing.Shared;
using Xunit;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ConvertChangeTypeResultConverterTests
    {
        [Theory(DisplayName = "The ConvertToDesiredType method should be able to convert the string '42' into the Int32 42."), AutoMoqData]
        public void ConvertToDesiredTypeCanConvertAStringNumberToInt32(ConvertChangeTypeResultConverter sut)
        {
            Assert.Equal(42, sut.ConvertToDesiredType("42", typeof(int)));
        }
    }
}