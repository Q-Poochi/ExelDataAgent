using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using DataAgent.Application.Features.StartAnalysis;
using DataAgent.Application.Features.GetJobStatus;
using DataAgent.Application.Features.AnalysisCallback;
using DataAgent.Application.Features.Emails.SendReportEmail;
using DataAgent.Application.DTOs;
using DataAgent.Application.Interfaces;

namespace DataAgent.API.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAnalysisNotificationService _notificationService;

    public AnalysisController(IMediator mediator, IRateLimitService rateLimitService, IAnalysisNotificationService notificationService)
    {
        _mediator = mediator;
        _rateLimitService = rateLimitService;
        _notificationService = notificationService;
    }

    [HttpPost("start")]
    [EnableRateLimiting("AnalysisPolicy")]
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

    [HttpPost("{jobId}/send-email")]
    [EnableRateLimiting("EmailPolicy")]
    public async Task<ActionResult> SendEmail(Guid jobId, [FromBody] SendEmailRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientEmail) || !request.RecipientEmail.Contains("@"))
            return BadRequest("Invalid email format");

        var command = new SendReportEmailCommand
        {
            JobId = jobId,
            RecipientEmail = request.RecipientEmail,
            RecipientName = request.RecipientName
        };

        var result = await _mediator.Send(command);
        return Ok(new { success = result, message = "Email has been queued for sending." });
    }

    [HttpPost("{jobId}/email-callback")]
    public async Task<ActionResult> EmailCallback(Guid jobId, [FromBody] dynamic payload)
    {
        // Thường n8n sẽ gọi vào đây với thông báo gửi email thành công để update SignalR / DB
        // Tạm thời coi việc n8n gọi là thành công mà không check signature phức tạp tùy yêu cầu.
        
        await _notificationService.NotifyEmailSent(jobId.ToString(), "N8N Auto Notification");
        return Ok(new { success = true });
    }
}
