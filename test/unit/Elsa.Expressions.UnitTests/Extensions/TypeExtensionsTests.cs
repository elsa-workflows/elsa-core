using Elsa.Expressions.Models;
using Elsa.Extensions;
using System.Threading.Tasks;

namespace Elsa.Expressions.UnitTests.Extensions;

public class TypeExtensionsTests
{
    [Test]
    public async Task GetFriendlyTypeName_NonGenericType_ReturnsFullName()
    {
        var result = typeof(string).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo(typeof(string).FullName);
    }

    [Test]
    public async Task GetFriendlyTypeName_GenericType_ReturnsFriendlyName()
    {
        var result = typeof(List<string>).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo("System.Collections.Generic.List<System.String>");
    }

    [Test]
    public async Task GetFriendlyTypeName_ArrayOfGenericType_ReturnsFriendlyName()
    {
        var result = typeof(List<string>[]).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo("System.Collections.Generic.List<System.String>[]");
    }

    [Test]
    public async Task GetFriendlyTypeName_ArrayOfNonGenericType_ReturnsFullNameWithBrackets()
    {
        var result = typeof(string[]).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo("System.String[]");
    }

    [Test]
    public async Task GetFriendlyTypeName_NestedGenericArray_ReturnsFriendlyName()
    {
        var result = typeof(Dictionary<string, int>[]).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo("System.Collections.Generic.Dictionary<System.String, System.Int32>[]");
    }

    [Test]
    public async Task GetFriendlyTypeName_MultiDimensionalArray_PreservesRank()
    {
        var result = typeof(int[,]).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo("System.Int32[,]");
    }

    [Test]
    public async Task GetFriendlyTypeName_MultiDimensionalGenericArray_PreservesRank()
    {
        var result = typeof(List<string>[,]).GetFriendlyTypeName(Brackets.Angle);
        await Assert.That(result).IsEqualTo("System.Collections.Generic.List<System.String>[,]");
    }

    [Test]
    public async Task GetFriendlyTypeName_SquareBrackets_UsesCorrectBrackets()
    {
        var result = typeof(List<string>).GetFriendlyTypeName(Brackets.Square);
        await Assert.That(result).IsEqualTo("System.Collections.Generic.List[System.String]");
    }
}
