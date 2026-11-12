using System.Security.Claims;

namespace Ticketly.Api.Auth;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Token has no sub claim.");

    public static bool IsStaff(this ClaimsPrincipal user) =>
        user.IsInRole("support") || user.IsInRole("admin");
}