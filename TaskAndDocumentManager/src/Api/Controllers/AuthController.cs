using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Auth.UseCases;

namespace TaskAndDocumentManager.Controllers
{
    [Route("auth")]
    public class AuthController: Controller
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
        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(string email, string password)
        {
            try
            {
                _registerUser.Execute(email, password);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login(string email, string password)
        {
            try
            {
                var authResult = _authenticateUser.Execute(email, password);
                return Ok(authResult);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
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

            try
            {
                var currentUser = _getCurrentUser.Execute(userId);
                return Ok(currentUser);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("users/{id:int}/deactivate")]
        public IActionResult Deactivate(int id)
        {
            try
            {
                _deactivateUser.Execute(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
