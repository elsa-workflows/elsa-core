namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>Builds the <see cref="BpmnErrorResponse"/> every coded BPMN refusal is sent as.</summary>
internal static class BpmnErrorResponseFactory
{
    /// <summary>
    /// The key FastEndpoints' own <c>AddError(message)</c> groups a message-only failure under — camelCased from
    /// its <c>Config.ErrOpts.GeneralErrorsField</c> default of <c>"GeneralErrors"</c>, which nothing in this
    /// deployment overrides (see <c>Elsa.FastEndpointConfigurators.ElsaFastEndpointsConfigurator</c>).
    /// </summary>
    private const string GeneralErrorsKey = "generalErrors";

    /// <summary>Builds the response for a single-message refusal, in the same shape <c>AddError(message)</c> would have produced.</summary>
    public static BpmnErrorResponse Create(string message, string code, int statusCode, object? data = null) => new()
    {
        StatusCode = statusCode,
        Errors = new Dictionary<string, IReadOnlyList<string>> { [GeneralErrorsKey] = [message] },
        Code = code,
        Data = data
    };
}
