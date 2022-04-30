using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> DeleteAsync(string definitionId, ICommandSender commandSender, CancellationToken cancellationToken)
    {
        var result = await commandSender.ExecuteAsync(new DeleteWorkflowDefinition(definitionId), cancellationToken);
        return !result ? Results.NotFound() : Results.NoContent();
    }
}