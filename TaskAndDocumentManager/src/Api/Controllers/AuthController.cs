using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Auth.UseCases;
namespace TaskAndDocumentManager.Controllers
{
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
        public IActionResult Register(string emial, string password)
        {
            _registerUser.Execute(emial, password);
            return RedirectToAction("Index", "Home");
        }
    }
}