using Elsa.Secrets.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;

namespace Elsa.Secrets.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/secrets")]
    [Produces("application/json")]
    public class Post : Controller
    {
        public Post()
        { 
            
        }

        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Secret))]
        //[SwaggerResponseExample(StatusCodes.Status200OK, typeof(Secret))]
        //public async Task<IActionResult> Post([FromBody] Secret request)
        //{ 
            
        //}
    }
}
