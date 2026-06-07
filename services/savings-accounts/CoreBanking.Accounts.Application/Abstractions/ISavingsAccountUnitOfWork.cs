namespace CoreBanking.Accounts.Application.Abstractions;

public interface ISavingsAccountUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
