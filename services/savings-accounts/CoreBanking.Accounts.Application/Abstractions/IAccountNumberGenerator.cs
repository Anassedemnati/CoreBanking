namespace CoreBanking.Accounts.Application.Abstractions;

/// <summary>
/// Produces a unique, human-readable account number for a new savings account.
/// Implementations must be safe under concurrent submissions.
/// </summary>
public interface IAccountNumberGenerator
{
    Task<string> NextAsync(CancellationToken ct = default);
}
