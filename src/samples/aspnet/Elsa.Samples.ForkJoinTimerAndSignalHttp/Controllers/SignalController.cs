﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.ForkJoinTimerAndSignalHttp.Controllers
{
    [ApiController]
    [Route("signal/{signalName}/trigger")]
    public class SignalController : Controller
    {
        private readonly ISignaler _signaler;

        public SignalController(ISignaler signaler)
        {
            _signaler = signaler;
        }
        
        [HttpGet]
        public async Task<IActionResult> Trigger(string signalName, string correlationId, CancellationToken cancellationToken)
        {
            await _signaler.SendSignalAsync(signalName, correlationId: correlationId, cancellationToken: cancellationToken);
            return Ok("Signal triggered :)");
        }
    }
}