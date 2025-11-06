using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<IdentityUser> login;
        private readonly UserManager<IdentityUser> user;

        public LoginController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            login = signInManager;
            user = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            var user = await this.user.FindByNameAsync(username);
            if (user == null)
            {
                ModelState.AddModelError("", "Niepoprawny login");
                return View();
            }
            var result = await login.PasswordSignInAsync(user, password, false, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Dishes");
            }

            ModelState.AddModelError("", "Niepoprawne hasło");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await login.SignOutAsync();
            return RedirectToAction("Index", "Dishes");
        }
    }
}
