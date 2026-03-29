using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using DataAgent.Application.Features.StartAnalysis;
using DataAgent.Application.Features.GetJobStatus;
using DataAgent.Application.Features.AnalysisCallback;
using DataAgent.Application.DTOs;

namespace DataAgent.API.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("start")]
    public async Task<ActionResult> StartAnalysis([FromBody] StartAnalysisCommand command)
    {
        var jobId = await _mediator.Send(command);
        return Ok(new { JobId = jobId });
    }

    [HttpGet("{jobId}/status")]
    public async Task<ActionResult<JobStatusDto>> GetJobStatus(Guid jobId)
    {
        var query = new GetJobStatusQuery(jobId);
        var response = await _mediator.Send(query);

        if (response == null)
            return NotFound();

        return Ok(response);
    }

    [HttpPost("callback")]
    public async Task<ActionResult> AnalysisCallback()
    {
        if (!Request.Headers.TryGetValue("X-Callback-Signature", out var signature))
        {
            return Unauthorized("Missing X-Callback-Signature header.");
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawPayload = await reader.ReadToEndAsync();

        var command = new AnalysisCallbackCommand
        {
            RawPayload = rawPayload,
            Signature = signature.ToString()
        };

        var result = await _mediator.Send(command);
        
        if (!result) return BadRequest("Could not process callback.");

        return Ok();
    }
}
