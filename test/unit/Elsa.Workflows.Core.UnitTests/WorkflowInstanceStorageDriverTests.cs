using System.IO;
using System.Threading;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elsa.Workflows.Core.UnitTests;

public class WorkflowInstanceStorageDriverTests
{
    private readonly Mock<ILogger<WorkflowInstanceStorageDriver>> _loggerMock;
    private readonly WorkflowInstanceStorageDriver _driver;

    public WorkflowInstanceStorageDriverTests()
    {
        _loggerMock = new Mock<ILogger<WorkflowInstanceStorageDriver>>();
        _driver = new WorkflowInstanceStorageDriver(null, _loggerMock.Object);
    }

    [Fact]
    public async Task WriteAsync_WithClosedStream_ShouldNotThrowException()
    {
        // Arrange
        var stream = new MemoryStream();
        stream.WriteByte(1);
        stream.Close(); // Close the stream to make it non-serializable
        
        var context = CreateMinimalStorageDriverContext();
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => 
            await _driver.WriteAsync("testId", stream, context));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task WriteAsync_WithClosedStream_ShouldLogWarning()
    {
        // Arrange
        var stream = new MemoryStream();
        stream.WriteByte(1);
        stream.Close(); // Close the stream to make it non-serializable
        
        var context = CreateMinimalStorageDriverContext();
        
        // Act
        await _driver.WriteAsync("testId", stream, context);
        
        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to serialize variable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteAsync_WithSerializableObject_ShouldSucceed()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };
        var context = CreateMinimalStorageDriverContext();
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => 
            await _driver.WriteAsync("testId", testObject, context));
        
        Assert.Null(exception);
    }

    private StorageDriverContext CreateMinimalStorageDriverContext()
    {
        var variable = new Variable<object>("testVariable");
        var executionContext = new Mock<IExecutionContext>();
        executionContext.Setup(x => x.Properties).Returns(new Dictionary<string, object>());
        
        return new StorageDriverContext(executionContext.Object, variable, CancellationToken.None);
    }
}