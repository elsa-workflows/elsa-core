using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;

namespace Elsa.Activities.UnitTests.Primitives;

/// <summary>
/// Unit tests for the <see cref="Fault"/> activity.
/// </summary>
public class FaultTests
{
    [Fact(DisplayName = "Fault throws FaultException with all properties set")]
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
        Assert.Equal("ERR_001", exception.Code);
        Assert.Equal("HTTP", exception.Category);
        Assert.Equal("Business", exception.Type);
        Assert.Equal("Invalid request", exception.Message);
    }

    [Fact(DisplayName = "Fault uses default values when inputs are null")]
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
        Assert.Equal("0", exception.Code);
        Assert.Equal("General", exception.Category);
        Assert.Equal("System", exception.Type);
        Assert.NotNull(exception.Message);
    }

    [Fact(DisplayName = "Fault throws FaultException with null message")]
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
        Assert.Equal("ERR_002", exception.Code);
        Assert.Equal("Database", exception.Category);
        Assert.Equal("Integration", exception.Type);
        Assert.NotNull(exception.Message);
    }

    [Theory(DisplayName = "Fault throws FaultException with various code values")]
    [InlineData("404", "404")]
    [InlineData("VALIDATION_ERROR", "VALIDATION_ERROR")]
    [InlineData("", "")]
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
        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact(DisplayName = "Fault.Create factory method creates correctly configured instance")]
    public async Task Create_Factory_Method_Creates_Correctly_Configured_Instance()
    {
        // Arrange
        var fault = Fault.Create("CREATE_001", "Factory", "Test", "Created via factory");

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        Assert.Equal("CREATE_001", exception.Code);
        Assert.Equal("Factory", exception.Category);
        Assert.Equal("Test", exception.Type);
        Assert.Equal("Created via factory", exception.Message);
    }

    [Fact(DisplayName = "Fault.Create factory method with null message")]
    public async Task Create_Factory_Method_With_Null_Message()
    {
        // Arrange
        var fault = Fault.Create("CODE", "Category", "Type");

        // Act & Assert
        var exception = await ExecuteAndAssertFaultAsync(fault);
        Assert.Equal("CODE", exception.Code);
        Assert.Equal("Category", exception.Category);
        Assert.Equal("Type", exception.Type);
        Assert.NotNull(exception.Message);
    }

    private static async Task<FaultException> ExecuteAndAssertFaultAsync(IActivity activity)
    {
        return await Assert.ThrowsAsync<FaultException>(() => ExecuteAsync(activity));
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}
