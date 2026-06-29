using System.Data;
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Services;

/// <summary>
/// Generates account numbers from the Oracle sequence <c>SAVINGS.SAVINGS_ACCOUNT_NO_SEQ</c>.
/// Each <c>NEXTVAL</c> is atomic and unique across sessions and service instances, so
/// concurrent submissions can never receive the same value (the unique index on
/// <c>ACCOUNTNO</c> is a further backstop). Numbers are zero-padded for readability.
/// </summary>
/// <remarks>
/// Uses a raw ADO.NET command rather than EF's <c>SqlQuery</c> because EF wraps scalar
/// SQL in a derived table, and Oracle rejects <c>NEXTVAL</c> inside a subquery (ORA-02287).
/// </remarks>
public sealed class AccountNumberGenerator(SavingsAccountsWriteDbContext db) : IAccountNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(ct);

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT \"SAVINGS\".\"SAVINGS_ACCOUNT_NO_SEQ\".NEXTVAL FROM dual";
            var result = await cmd.ExecuteScalarAsync(ct);
            var next = Convert.ToInt64(result);
            return next.ToString("D9");
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
