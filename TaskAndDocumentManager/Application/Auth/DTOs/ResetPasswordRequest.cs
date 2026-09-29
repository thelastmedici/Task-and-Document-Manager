namespace TaskAndDocumentManager.Application.Auth.DTOs;

public class ResetPasswordRequest
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}
