using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace CoreBanking.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Maps Keycloak realm roles from the <c>realm_access.roles</c> JWT claim
/// to the standard <see cref="ClaimTypes.Role"/> claim so that
/// <c>[Authorize(Roles = "...")]</c> works as expected.
/// </summary>
public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var realmAccess = identity.FindFirst("realm_access");
        if (realmAccess is null)
            return Task.FromResult(principal);

        using var doc = JsonDocument.Parse(realmAccess.Value);
        if (!doc.RootElement.TryGetProperty("roles", out var rolesElement))
            return Task.FromResult(principal);

        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleName = role.GetString();
            if (roleName is not null && !identity.HasClaim(ClaimTypes.Role, roleName))
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        }

        return Task.FromResult(principal);
    }
}
