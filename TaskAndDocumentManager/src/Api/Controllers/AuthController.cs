using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Api.Authorization;
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
    private readonly ListUsers _listUsers;
    private readonly CreateUserAsAdmin _createUserAsAdmin;
    private readonly ChangeUserRole _changeUserRole;
    private readonly DeleteUser _deleteUser;

    public AuthController(
        RegisterUser registerUser,
        AuthenticateUser authenticateUser,
        GetCurrentUser getCurrentUser,
        DeactivateUser deactivateUser,
        ChangeUserRole changeUserRole,
        ListUsers listUsers,
        CreateUserAsAdmin createUserAsAdmin,
        DeleteUser deleteUser
        )
    {
        _registerUser = registerUser;
        _authenticateUser = authenticateUser;
        _getCurrentUser = getCurrentUser;
        _deactivateUser = deactivateUser;
        _changeUserRole = changeUserRole;
        _listUsers = listUsers;
        _createUserAsAdmin = createUserAsAdmin;
        _deleteUser = deleteUser;
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

    [Authorize(Policy = AppPolicies.Authenticated)]
    [HttpGet("me")]
    public IActionResult Me()
    {
        try
        {
            var userId = User.GetActorId();
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

    [Authorize(Policy = AppPolicies.AdminOnly)]
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


    [Authorize(Policy = AppPolicies.AdminOnly)]
[HttpGet("users")]
public IActionResult GetUsers()
{
    var users = _listUsers.Execute();
    return Ok(users);
}

[Authorize(Policy = AppPolicies.AdminOnly)]
[HttpPost("users")]
public IActionResult CreateUser([FromBody] CreateUserByAdminRequest request)
{
    try
    {
        _createUserAsAdmin.Execute(request.Email, request.Password, request.RoleId);
        return StatusCode(StatusCodes.Status201Created, new { message = "User created successfully." });
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
}

[Authorize(Policy = AppPolicies.AdminOnly)]
[HttpPut("users/{id:guid}/role")]
public IActionResult ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
{
    try
    {
        _changeUserRole.Execute(id, request.RoleId);
        return NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

[Authorize(Policy = AppPolicies.AdminOnly)]
[HttpDelete("users/{id:guid}")]
public IActionResult Delete(Guid id)
{
    try
    {
        _deleteUser.Execute(id);
        return NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}

public sealed class CreateUserByAdminRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public Guid RoleId { get; init; }
}

public sealed class ChangeRoleRequest
{
    public Guid RoleId { get; init; }
}

}
