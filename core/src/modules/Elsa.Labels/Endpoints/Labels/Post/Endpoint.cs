using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using Elsa.Workflows;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Labels.Endpoints.Labels.Post;

[PublicAPI]
internal class Create : ElsaEndpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Create(ILabelStore store, IIdentityGenerator identityGenerator)
    {
        _store = store;
    }

    public override void Configure()
    {
        Post("/labels");
        RequirePermission(Elsa.Labels.Permissions.LabelPermissions.Labels, CoreVerbs.Create);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (LabelNameValidator.IsTooLong(request.Name))
        {
            AddError(LabelNameValidator.TooLongMessage);
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        if (await LabelNameValidator.IsDuplicateAsync(_store, request.Name, null, cancellationToken))
        {
            AddError(LabelNameValidator.DuplicateMessage);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        var label = Map.ToEntity(request);
        await _store.SaveAsync(label, cancellationToken);
        var response = Map.FromEntity(label);
        await Send.CreatedAtAsync<Get.Get>(new { Id = label.Id }, response, cancellation: cancellationToken);
    }
}