using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.Clients.Application;
using CoreBanking.Clients.Application.Clients;
using CoreBanking.Clients.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoreBanking.Clients.UnitTests;

public sealed class RegisterClientHandlerTests
{
    [Fact]
    public async Task Handle_persists_client_and_returns_id()
    {
        var repo = Substitute.For<IClientRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new RegisterClientHandler(repo, uow);

        var id = await handler.Handle(new RegisterClientCommand("Ada Lovelace", "EXT-1"), default);

        repo.Received(1).Add(Arg.Is<Client>(c => c.DisplayName == "Ada Lovelace"));
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ActivateClient_loads_client_activates_and_saves()
    {
        var client = Client.Register("Bob", null);
        var repo = Substitute.For<IClientRepository>();
        repo.FindAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new ActivateClientHandler(repo, uow);

        await handler.Handle(new ActivateClientCommand(client.Id, new DateOnly(2026, 6, 8)), default);

        client.Status.Should().Be(ClientStatus.Active);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateClient_throws_NotFoundException_when_not_found()
    {
        var repo = Substitute.For<IClientRepository>();
        repo.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new ActivateClientHandler(repo, uow);

        var act = async () => await handler.Handle(new ActivateClientCommand(Guid.NewGuid(), new DateOnly(2026, 6, 8)), default);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetClientById_returns_dto_or_throws_NotFoundException()
    {
        var client = Client.Register("Alice", "EXT-2");
        var readRepo = Substitute.For<IClientReadRepository>();
        readRepo.FindDtoAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new ClientDto(client.Id, client.DisplayName, client.ExternalId, "Pending", null));
        var handler = new GetClientByIdHandler(readRepo);

        var dto = await handler.Handle(new GetClientByIdQuery(client.Id), default);
        dto.DisplayName.Should().Be("Alice");
    }
}
