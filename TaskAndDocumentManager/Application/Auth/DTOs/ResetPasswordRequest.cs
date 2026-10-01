namespace TaskAndDocumentManager.Application.Auth.DTOs;

public class ResetPasswordRequest
{
    public string? Token { get; set; }
    public string? Email { get; set; }
    public required string NewPassword { get; set; }
}
