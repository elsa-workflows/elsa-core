using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Labels.Endpoints.Labels.Update;

[PublicAPI]
internal class Update(ILabelStore store) : ElsaEndpoint<Request, Response, LabelMapper>
{
    public override void Configure()
    {
        Post("/labels/{id}");
        RequirePermission(Elsa.Labels.Permissions.LabelPermissions.Labels, CoreVerbs.Update);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = await store.FindByIdAsync(request.Id, cancellationToken);

        if (label == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (LabelNameValidator.IsTooLong(request.Name))
        {
            AddError(LabelNameValidator.TooLongMessage);
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        if (await LabelNameValidator.IsDuplicateAsync(store, request.Name, label.Id, cancellationToken))
        {
            AddError(LabelNameValidator.DuplicateMessage);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        label = Map.UpdateEntity(request, label);

        await store.SaveAsync(label, cancellationToken);
        var response = Map.FromEntity(label);
        await Send.OkAsync(response, cancellationToken);
    }
}