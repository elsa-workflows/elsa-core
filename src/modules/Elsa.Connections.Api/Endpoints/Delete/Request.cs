using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Connections.Api.Endpoints.Delete;
public class Request
{    [Required] public string Id { get; set; } = default!;
}
