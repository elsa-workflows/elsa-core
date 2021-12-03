using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Services.Models;

namespace Elsa.Scripting.JavaScript.Providers
{
    public interface IActivityTypeDefinitionRenderer
    {
        int Priority { get; }
        bool GetCanRenderType(ActivityType activityType);
        ValueTask RenderTypeDeclarationAsync(
            RenderingTypeScriptDefinitions notification,
            ActivityType activityType,
            ActivityDescriptor activityDescriptor,
            ActivityDefinition activityDefinition,
            StringBuilder writer,
            CancellationToken cancellationToken = default);
    }
}