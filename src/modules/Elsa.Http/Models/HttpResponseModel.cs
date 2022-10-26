using System.Collections.Generic;
using System.Net;

namespace Elsa.Http.Models;

public record HttpResponseModel(HttpStatusCode StatusCode, IDictionary<string, string[]> Headers);