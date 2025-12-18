// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Worktrack.Services;

namespace Worktrack.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly UserService UService;
        public AuthController(UserService users)
        {
            UService = users;
        }
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("AuthController alive");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return Redirect("/user/login");
        }
         [HttpPost("login")]
    public async Task<IActionResult> Login(string SecretCode)
        {
        if (string.IsNullOrWhiteSpace(SecretCode))
        {
            TempData["Error"] = "Secret Code fehlt";
            return Redirect("/user/login");
        }
        var user = await UService.ValidateSecretCodeAsync(SecretCode);
        if (user is null)
        {
            ViewData["Error"] = "Login fehlgeschlagen";
            return Redirect("/user/login");
        }
         var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "CookieAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("CookieAuth", principal);

        return Redirect("/user/dashboard");
        }
    }
}