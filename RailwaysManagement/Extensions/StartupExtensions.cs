using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace RailwaysManagement.Extensions;

public static class StartupExtensions
{
    public static void MapAccountServices(this IEndpointRouteBuilder app)
    {
        app
            .MapGet("/Logout", async (HttpContext context, string returnUrl = "/") =>
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                context.Response.Redirect(returnUrl);
            })
            .RequireAuthorization();
    }
}