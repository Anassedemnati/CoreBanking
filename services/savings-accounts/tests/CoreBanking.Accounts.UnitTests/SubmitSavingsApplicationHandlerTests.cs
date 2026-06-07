using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Application.ReadModels;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SubmitSavingsApplicationHandlerTests
{
    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 6);

    private static SubmitSavingsApplicationCommand ValidCommand() =>
        new(ClientId, ProductId, "SA-0001", "USD", 2, 5.0m, Today);

    private static IClientRefRepository ActiveClientRepo()
    {
        var repo = Substitute.For<IClientRefRepository>();
        repo.FindAsync(ClientId, Arg.Any<CancellationToken>())
            .Returns(new ClientRef { ClientId = ClientId, DisplayName = "Ada", IsActive = true });
        return repo;
    }

    private static IProductRefRepository ValidProductRepo()
    {
        var repo = Substitute.For<IProductRefRepository>();
        repo.FindAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new ProductRef
            {
                ProductId = ProductId, Name = "Basic Savings",
                CurrencyCode = "USD", CurrencyDecimalPlaces = 2, DefaultRate = 5.0m
            });
        return repo;
    }

    [Fact]
    public async Task Submit_handler_validates_client_is_active()
    {
        var clientRepo = Substitute.For<IClientRefRepository>();
        clientRepo.FindAsync(ClientId, Arg.Any<CancellationToken>())
            .Returns(new ClientRef { ClientId = ClientId, DisplayName = "Ada", IsActive = false });

        var handler = new SubmitSavingsApplicationHandler(
            Substitute.For<ISavingsAccountRepository>(),
            Substitute.For<ISavingsAccountUnitOfWork>(),
            clientRepo,
            ValidProductRepo());

        var act = async () => await handler.Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Submit_handler_throws_when_client_not_found()
    {
        var clientRepo = Substitute.For<IClientRefRepository>();
        clientRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ClientRef?)null);

        var handler = new SubmitSavingsApplicationHandler(
            Substitute.For<ISavingsAccountRepository>(),
            Substitute.For<ISavingsAccountUnitOfWork>(),
            clientRepo,
            ValidProductRepo());

        var act = async () => await handler.Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Approve_handler_loads_account_approves_and_saves()
    {
        var account = SavingsAccount.SubmitApplication(
            ClientId, ProductId, "SA-0001", "USD", 2, 5.0m, Today);

        var repo = Substitute.For<ISavingsAccountRepository>();
        repo.FindAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var handler = new ApproveSavingsAccountHandler(repo, uow);

        await handler.Handle(new ApproveSavingsAccountCommand(account.Id, Today), default);

        account.Status.Should().Be(SavingsAccountStatus.Approved);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
