using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Workflows.Exceptions;
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

        var nameError = LabelNameValidator.GetNameError(request.Name);

        if (nameError != null)
        {
            AddError(nameError);
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var nameChanged = Label.NormalizeName(request.Name) != label.NormalizedName;

        if (nameChanged && await LabelNameValidator.IsDuplicateAsync(store, request.Name, label.Id, cancellationToken))
        {
            AddError(LabelNameValidator.DuplicateMessage);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        label = Map.UpdateEntity(request, label);

        try
        {
            await store.SaveAsync(label, cancellationToken);
        }
        catch (UniqueKeyConstraintViolationException)
        {
            // A concurrent create or rename won the race for the same name.
            AddError(LabelNameValidator.DuplicateMessage);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }
        var response = Map.FromEntity(label);
        await Send.OkAsync(response, cancellationToken);
    }
}