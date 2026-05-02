using Elsa.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Elsa.Common.UnitTests;

public class ExceptionExtensionsTests
{
    [Fact(DisplayName = "Null is not fatal")]
    public void NullNotFatal() => Assert.False(((Exception?)null).IsFatal());

    [Theory(DisplayName = "Process-fatal exceptions are classified fatal")]
    [InlineData(typeof(StackOverflowException))]
    [InlineData(typeof(AccessViolationException))]
    [InlineData(typeof(SEHException))]
    [InlineData(typeof(ThreadAbortException))]
    public void FatalExceptions(Type exceptionType)
    {
        var ex = (Exception)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(exceptionType);
        Assert.True(ex.IsFatal());
    }

    [Fact(DisplayName = "OutOfMemoryException is fatal")]
    public void OomFatal() => Assert.True(new OutOfMemoryException().IsFatal());

    [Fact(DisplayName = "InsufficientMemoryException (recoverable subclass of OOM) is NOT fatal")]
    public void InsufficientMemoryNotFatal() => Assert.False(new InsufficientMemoryException().IsFatal());

    [Theory(DisplayName = "Common recoverable exceptions are NOT fatal")]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(NullReferenceException))]
    [InlineData(typeof(IOException))]
    public void RecoverableExceptions(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.False(ex.IsFatal());
    }

    [Fact(DisplayName = "TypeInitializationException wrapping a fatal cause is itself fatal")]
    public void WrappedFatalIsFatal()
    {
        var inner = new StackOverflowException();
        var outer = new TypeInitializationException("X", inner);
        Assert.True(outer.IsFatal());
    }

    [Fact(DisplayName = "TargetInvocationException wrapping a recoverable cause is NOT fatal")]
    public void WrappedRecoverableIsNotFatal()
    {
        var inner = new InvalidOperationException("boom");
        var outer = new TargetInvocationException(inner);
        Assert.False(outer.IsFatal());
    }
}
