using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Primitives;

/// <summary>
/// Unit tests for the <see cref="Fault"/> activity.
/// </summary>
public class FaultTests
{
    [Test]
    [DisplayName("Fault throws FaultException with all properties set")]
    public async Task Should_Throw_FaultException_With_All_Properties()
    {
        // Arrange
        var fault = new Fault
        {
            Code = new("ERR_001"),
            Category = new("HTTP"),
            FaultType = new("Business"),
            Message = new("Invalid request")
        };

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        await Assert.That(exception.Code).IsEqualTo("ERR_001");
        await Assert.That(exception.Category).IsEqualTo("HTTP");
        await Assert.That(exception.Type).IsEqualTo("Business");
        await Assert.That(exception.Message).IsEqualTo("Invalid request");
    }

    [Test]
    [DisplayName("Fault uses default values when inputs are null")]
    public async Task Should_Use_Default_Values_When_Inputs_Are_Null()
    {
        // Arrange
        var fault = new Fault
        {
            Code = new((string)null!),
            Category = new((string)null!),
            FaultType = new((string)null!),
            Message = new((string?)null)
        };

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        await Assert.That(exception.Code).IsEqualTo("0");
        await Assert.That(exception.Category).IsEqualTo("General");
        await Assert.That(exception.Type).IsEqualTo("System");
        await Assert.That(exception.Message).IsNotNull();
    }

    [Test]
    [DisplayName("Fault throws FaultException with null message")]
    public async Task Should_Throw_FaultException_With_Null_Message()
    {
        // Arrange
        var fault = new Fault
        {
            Code = new("ERR_002"),
            Category = new("Database"),
            FaultType = new("Integration"),
            Message = new((string?)null)
        };

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        await Assert.That(exception.Code).IsEqualTo("ERR_002");
        await Assert.That(exception.Category).IsEqualTo("Database");
        await Assert.That(exception.Type).IsEqualTo("Integration");
        await Assert.That(exception.Message).IsNotNull();
    }

    [Test]
    [DisplayName("Fault throws FaultException with various code values")]
    [Arguments("404", "404")]
    [Arguments("VALIDATION_ERROR", "VALIDATION_ERROR")]
    [Arguments("", "")]
    public async Task Should_Throw_FaultException_With_Various_Codes(string code, string expectedCode)
    {
        // Arrange
        var fault = new Fault
        {
            Code = new(code),
            Category = new("Test"),
            FaultType = new("Test")
        };

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        await Assert.That(exception.Code).IsEqualTo(expectedCode);
    }

    [Test]
    [DisplayName("Fault.Create factory method creates correctly configured instance")]
    public async Task Create_Factory_Method_Creates_Correctly_Configured_Instance()
    {
        // Arrange
        var fault = Fault.Create("CREATE_001", "Factory", "Test", "Created via factory");

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        await Assert.That(exception.Code).IsEqualTo("CREATE_001");
        await Assert.That(exception.Category).IsEqualTo("Factory");
        await Assert.That(exception.Type).IsEqualTo("Test");
        await Assert.That(exception.Message).IsEqualTo("Created via factory");
    }

    [Test]
    [DisplayName("Fault.Create factory method with null message")]
    public async Task Create_Factory_Method_With_Null_Message()
    {
        // Arrange
        var fault = Fault.Create("CODE", "Category", "Type");

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        await Assert.That(exception.Code).IsEqualTo("CODE");
        await Assert.That(exception.Category).IsEqualTo("Category");
        await Assert.That(exception.Type).IsEqualTo("Type");
        await Assert.That(exception.Message).IsNotNull();
    }

    private static async Task<FaultException> ExecuteAndAssertFaultAsync(IActivity activity)
    {
        return (await Assert.ThrowsExactlyAsync<FaultException>(() => ExecuteAsync(activity)))!;
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}
