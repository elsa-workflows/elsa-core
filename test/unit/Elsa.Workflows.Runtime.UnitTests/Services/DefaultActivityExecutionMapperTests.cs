using Elsa.Common;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Options;
using Moq;
using Elsa.Workflows.Activities;
using Elsa.Testing.Shared;
namespace Elsa.Workflows.Runtime.UnitTests.Services;
public class DefaultActivityExecutionMapperTests
{
    private readonly Mock<ISafeSerializer> _safeSerializerMock = new();
    private readonly Mock<IPayloadSerializer> _payloadSerializerMock = new();
    private readonly Mock<ICompressionCodecResolver> _compressionCodecResolverMock = new();
    private readonly Mock<IOptions<ManagementOptions>> _optionsMock = new();
    private readonly DefaultActivityExecutionMapper _mapper;

    public DefaultActivityExecutionMapperTests()
    {
        _optionsMock.Setup(x => x.Value).Returns(new ManagementOptions());
        _compressionCodecResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(new Mock<ICompressionCodec>().Object);
        
        _mapper = new(
            _safeSerializerMock.Object,
            _payloadSerializerMock.Object,
            _compressionCodecResolverMock.Object,
            _optionsMock.Object);
    }

    [Fact]
    public async Task MapAsync_SetsCallStackDepth_FromContext()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var w = contextRoot.WorkflowExecutionContext;
        var clock = contextRoot.GetRequiredService<ISystemClock>();
        
        var contextA = new ActivityExecutionContext("A", w, null, root, contextRoot.ActivityDescriptor, contextRoot.StartedAt, null, clock, default)
        {
            CallStackDepth = 5
        };

        // Act
        var record = await _mapper.MapAsync(contextA);

        // Assert
        Assert.Equal(5, record.CallStackDepth);
    }
}
