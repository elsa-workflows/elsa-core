namespace Elsa.Api.Client.Resources.Alterations.Responses;

/// <summary>
/// The response to the "Submit" endpoint
/// </summary>
public class SubmitResponse
{
    /// <summary>
    /// The ID of the alteration plan created as part of the Submit request
    /// </summary>
    public string PlanId { get; set; } = string.Empty;
}