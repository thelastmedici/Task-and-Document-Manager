using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Auth.UseCases;
namespace TaskAndDocumentManager.Controllers
{
    [Route("auth")]
    public class AuthController: Controller
    {
        private readonly RegisterUser _registerUser;
        public AuthController(RegisterUser registerUser)
        {
            _registerUser = registerUser;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
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
    }
}