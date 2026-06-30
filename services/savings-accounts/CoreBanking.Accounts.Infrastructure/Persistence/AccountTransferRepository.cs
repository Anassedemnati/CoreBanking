using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class AccountTransferRepository(SavingsAccountsWriteDbContext db) : IAccountTransferRepository
{
    public void Add(AccountTransfer transfer) => db.AccountTransfers.Add(transfer);

    public async Task<AccountTransfer?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await db.AccountTransfers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<AccountTransfer?> FindByClientReferenceAsync(
        string clientTransferReference, CancellationToken ct = default)
        => await db.AccountTransfers
            .Where(t => t.ClientTransferReference == clientTransferReference)
            .FirstOrDefaultAsync(ct);
}
