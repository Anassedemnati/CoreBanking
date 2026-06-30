using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Accounts.Api.Security;

/// <summary>
/// Resolves the current user from the JWT subject claim in the active HTTP request.
/// Falls back to "system" when there is no authenticated HTTP context (e.g. background services).
/// </summary>
/// <remarks>
/// Program.cs sets <c>MapInboundClaims = false</c>, so the JWT "sub" claim retains its original
/// name rather than being remapped to <c>ClaimTypes.NameIdentifier</c>.
/// </remarks>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId =>
        accessor.HttpContext?.User?.FindFirst("sub")?.Value
        ?? "system";
}
