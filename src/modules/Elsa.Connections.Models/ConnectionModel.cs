using System.ComponentModel.DataAnnotations;

namespace Elsa.Connections.Models;

public class ConnectionModel : ConnectionInputModel
{
    /// <summary>
    /// The Id of the Connection
    /// </summary>
    [Required] public string Id { get; set; } = null!;
}