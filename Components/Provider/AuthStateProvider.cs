using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Worktrack.Services;
using Worktrack.Models;

namespace Worktrack.Components.Provider;
public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor HttpContextAccessor;
    private readonly UserService UService;
    private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public AuthStateProvider(IHttpContextAccessor httpContextAccessor, UserService userService)
    {
        HttpContextAccessor = httpContextAccessor;
        UService = userService;
    }
    public async Task SignInAsync(string username, string role ,int id)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserId", id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "CookieAuth");
        var principal = new ClaimsPrincipal(identity);
        Console.WriteLine("LOGIN ENDPOINT REACHED");

        try
        {
            
            await HttpContextAccessor.HttpContext.SignInAsync("CookieAuth", principal,new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(15)
            });
            Console.WriteLine("SIGNIN CALLED");
        }
        catch (Exception ex)
        {
            Console.WriteLine("SIGNIN ERROR: " + ex.Message);
        }
    }

    public async Task SignOutAsync()
    {
        await HttpContextAccessor.HttpContext!.SignOutAsync("CookieAuth");
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("UserId empty");
            return null;
        }
        return await UService.GetUserByStringId(userId);
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
    
        var authenticateResult = await httpContext.AuthenticateAsync("CookieAuth");
        if (!authenticateResult.Succeeded) {
            Console.WriteLine("Failed to authenticate");
            return new AuthenticationState(_anonymous);
        }
    
        var user = authenticateResult.Principal;
        return new AuthenticationState(user);
    }

}