using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Elsa.Workflows.Activities;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
namespace Elsa.Workflows.Runtime.UnitTests.Services;
public class DefaultActivityExecutionMapperTests
{
    private readonly Mock<ISafeSerializer> _safeSerializerMock = new();
    private readonly Mock<IPayloadSerializer> _payloadSerializerMock = new();
    private readonly Mock<ICompressionCodecResolver> _compressionCodecResolverMock = new();
    private readonly Mock<IOptions<ManagementOptions>> _optionsMock = new();
    private readonly DefaultActivityExecutionMapper _sut;
 using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Options;
using Moq;
using Xupusing Elsa.Commont.using Elsa.Workflows.Mans(using Elsa.Workflows.Runtime;
using Mic  using Microsoft.Extensions.Oviusing Moq;
using Xunit;
using Elsafeusing XunrMusing Elsa.
 using Elsa.Workflows;
using Micckusing Microsoft.Exte _using System.Collections.Generic;
using System   _optionsMock.Object);
    }
    [using     public async Task Manamespace Elsa.Workflows.rectly_WhenCircularReferenceExists()
    {
        // A{
    private readonly Mock<ISafeSerializer> _s;
      private readonly Mock<IPayloadSerializer> _payloadSerializerMock =Ro    private readonly Mock<ICompressionCodecResolver> _compressionCodecResolvon    private readonly Mock<IOptions<ManagementOptions>> _optionsMock = new();
    private ron    private readonly DefaultActivityExecutionMapper _sut;
 using Elsa.Commoty using Elsa.Common;
using Elsa.Common.Codecs;
usiefault);
using Elsa.Common.tB = new ActivityExecutionCousing El", w, null, root, contextRoot.Actusing Microsoft.Extensions.O.Susing Moq;
using Xupusing Elsa.Com  using Xupreusing Mic  using Microsoft.Extensions.Oviusing Moq;
using Xunit;
using Elsafeusing  using Xunit;
using Elsafeusing XunrMusing Elsa.
    using Elsafis using Elsa.Workflows;
using Micc eusing Micckusing       using System   _optionsMock.Object);
    }
    [using   ActivityExe    }
    [using     public async T A          {
   record = await _sut.MapAsync(contextA);
        // Assert
        Assert.Equal(2, record.CallStackDepth);
    }
}
