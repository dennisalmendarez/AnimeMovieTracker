using AnimeMovieTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace AnimeMovieTracker.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (
            HttpContext httpContext,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var form = await httpContext.Request.ReadFormAsync();

            var email = form["email"].ToString();
            var password = form["password"].ToString();

            var result = await signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Results.Redirect("/dashboard");
            }

            return Results.Redirect("/login?error=1");
        });

        app.MapPost("/auth/logout", async (
            SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/");
        });
    }
}