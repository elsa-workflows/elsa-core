using System.Text.Json.Serialization;

namespace Elsa.AllInOne.Web.Models;

public record UserModel(
    [property: JsonPropertyName("id")]int Id, 
    [property: JsonPropertyName("email")]string Email, 
    [property: JsonPropertyName("first_name")]string FirstName, 
    [property: JsonPropertyName("last_name")]string LastName, 
    [property: JsonPropertyName("avatar")]string Avatar);