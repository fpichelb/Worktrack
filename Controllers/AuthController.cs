// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Worktrack.Services;

namespace Worktrack.Controllers
{
    [ApiController]
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
    }
}