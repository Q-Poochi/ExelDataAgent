namespace DataAgent.Application.DTOs;

public class SendEmailRequestDto
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
}
