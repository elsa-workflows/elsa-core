using System.Text.Json.Serialization;

namespace Elsa.WorkflowServer.Web;

public record ApiResponse<T>(T Data, Support Support);

public record Support(string Url, string Text);

public record User(
    int Id,
    [property: JsonPropertyName("first_name")]
    string FirstName,
    [property: JsonPropertyName("last_name")]
    string LastName,
    string Email,
    string Avatar);