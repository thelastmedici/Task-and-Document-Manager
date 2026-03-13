using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Auth.UseCases;

namespace TaskAndDocumentManager.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUser _registerUser;
    private readonly AuthenticateUser _authenticateUser;
    private readonly GetCurrentUser _getCurrentUser;
    private readonly DeactivateUser _deactivateUser;

    public AuthController(
        RegisterUser registerUser,
        AuthenticateUser authenticateUser,
        GetCurrentUser getCurrentUser,
        DeactivateUser deactivateUser)
    {
        _registerUser = registerUser;
        _authenticateUser = authenticateUser;
        _getCurrentUser = getCurrentUser;
        _deactivateUser = deactivateUser;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        try
        {
            _registerUser.Execute(request.Email, request.Password);

            return Ok(new { message = "User registered successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            var authResult = _authenticateUser.Execute(request.Email, request.Password);
            return Ok(authResult);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user id claim.");
        }

        var currentUser = _getCurrentUser.Execute(userId);
        return Ok(currentUser);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:int}/deactivate")]
    public IActionResult Deactivate(int id)
    {
        _deactivateUser.Execute(id);
        return NoContent();
    }
}