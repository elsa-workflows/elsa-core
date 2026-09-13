using Elsa.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests;

public class ExceptionExtensionsTests
{
    [Test]
    [DisplayName("Null is not fatal")]
    public async Task NullNotFatal() => await Assert.That(((Exception?)null).IsFatal()).IsFalse();

    [Test]
    [DisplayName("Process-fatal exceptions are classified fatal")]
    [Arguments(typeof(StackOverflowException))]
    [Arguments(typeof(AccessViolationException))]
    [Arguments(typeof(SEHException))]
    [Arguments(typeof(ThreadAbortException))]
    public async Task FatalExceptions(Type exceptionType)
    {
#pragma warning disable SYSLIB0050 // FormatterServices is needed to instantiate fatal exception types without throwing them.
        var ex = (Exception)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(exceptionType);
#pragma warning restore SYSLIB0050
        await Assert.That(ex.IsFatal()).IsTrue();
    }

    [Test]
    [DisplayName("OutOfMemoryException is fatal")]
    public async Task OomFatal() => await Assert.That(new OutOfMemoryException().IsFatal()).IsTrue();

    [Test]
    [DisplayName("InsufficientMemoryException (recoverable subclass of OOM) is NOT fatal")]
    public async Task InsufficientMemoryNotFatal() => await Assert.That(new InsufficientMemoryException().IsFatal()).IsFalse();

    [Test]
    [DisplayName("Common recoverable exceptions are NOT fatal")]
    [Arguments(typeof(InvalidOperationException))]
    [Arguments(typeof(ArgumentException))]
    [Arguments(typeof(TimeoutException))]
    [Arguments(typeof(NullReferenceException))]
    [Arguments(typeof(IOException))]
    public async Task RecoverableExceptions(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        await Assert.That(ex.IsFatal()).IsFalse();
    }

    [Test]
    [DisplayName("TypeInitializationException wrapping a fatal cause is itself fatal")]
    public async Task WrappedFatalIsFatal()
    {
        var inner = new StackOverflowException();
        var outer = new TypeInitializationException("X", inner);
        await Assert.That(outer.IsFatal()).IsTrue();
    }

    [Test]
    [DisplayName("TargetInvocationException wrapping a recoverable cause is NOT fatal")]
    public async Task WrappedRecoverableIsNotFatal()
    {
        var inner = new InvalidOperationException("boom");
        var outer = new TargetInvocationException(inner);
        await Assert.That(outer.IsFatal()).IsFalse();
    }
}