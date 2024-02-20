// See: https://github.com/dotnet/core/issues/8016
namespace System.Runtime.CompilerServices;

public class RequiredMemberAttribute : Attribute { }
public class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string name) { }
}