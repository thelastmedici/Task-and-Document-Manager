using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Api.Extensions;

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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred during registration." });
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
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        try
        {
            var userId = User.GetUserId();
            var currentUser = _getCurrentUser.Execute(userId);
            return Ok(currentUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}/deactivate")]
    public IActionResult Deactivate(Guid id)
    {
        try
        {
            _deactivateUser.Execute(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while deactivating the user." });
        }
    }
}
