using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.Expressions.UnitTests.Extensions;

public class TypeExtensionsTests
{
    [Fact]
    public void GetFriendlyTypeName_NonGenericType_ReturnsFullName()
    {
        var result = typeof(string).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal(typeof(string).FullName, result);
    }

    [Fact]
    public void GetFriendlyTypeName_GenericType_ReturnsFriendlyName()
    {
        var result = typeof(List<string>).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal("System.Collections.Generic.List<System.String>", result);
    }

    [Fact]
    public void GetFriendlyTypeName_ArrayOfGenericType_ReturnsFriendlyName()
    {
        var result = typeof(List<string>[]).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal("System.Collections.Generic.List<System.String>[]", result);
    }

    [Fact]
    public void GetFriendlyTypeName_ArrayOfNonGenericType_ReturnsFullNameWithBrackets()
    {
        var result = typeof(string[]).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal("System.String[]", result);
    }

    [Fact]
    public void GetFriendlyTypeName_NestedGenericArray_ReturnsFriendlyName()
    {
        var result = typeof(Dictionary<string, int>[]).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal("System.Collections.Generic.Dictionary<System.String, System.Int32>[]", result);
    }

    [Fact]
    public void GetFriendlyTypeName_MultiDimensionalArray_PreservesRank()
    {
        var result = typeof(int[,]).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal("System.Int32[,]", result);
    }

    [Fact]
    public void GetFriendlyTypeName_MultiDimensionalGenericArray_PreservesRank()
    {
        var result = typeof(List<string>[,]).GetFriendlyTypeName(Brackets.Angle);
        Assert.Equal("System.Collections.Generic.List<System.String>[,]", result);
    }

    [Fact]
    public void GetFriendlyTypeName_SquareBrackets_UsesCorrectBrackets()
    {
        var result = typeof(List<string>).GetFriendlyTypeName(Brackets.Square);
        Assert.Equal("System.Collections.Generic.List[System.String]", result);
    }
}
