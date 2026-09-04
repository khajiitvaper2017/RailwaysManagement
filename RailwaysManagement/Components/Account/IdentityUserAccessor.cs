using Microsoft.AspNetCore.Identity;
using RailwaysManagement.DbModels;

namespace RailwaysManagement.Components.Account
{
    internal sealed class IdentityUserAccessor(UserManager<RailwaysManagementUser> userManager, IdentityRedirectManager redirectManager)
    {
        public async Task<RailwaysManagementUser> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }

            return user;
        }
    }
}
