using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.Clients.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Clients.UnitTests;

public sealed class ClientTests
{
    [Fact]
    public void Register_creates_pending_client_and_raises_event()
    {
        var c = Client.Register("Ada Lovelace", externalId: "EXT-1");
        c.Status.Should().Be(ClientStatus.Pending);
        c.DomainEvents.OfType<ClientRegistered>().Should().ContainSingle();
    }

    [Fact]
    public void Activate_from_pending_sets_active_and_date()
    {
        var c = Client.Register("Ada", null);
        c.Activate(new DateOnly(2026, 6, 8));
        c.Status.Should().Be(ClientStatus.Active);
        c.ActivationDate.Should().Be(new DateOnly(2026, 6, 8));
    }

    [Fact]
    public void Activate_when_already_active_throws_DomainException()
    {
        var c = Client.Register("Ada", null);
        c.Activate(new DateOnly(2026, 6, 8));
        var act = () => c.Activate(new DateOnly(2026, 6, 9));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Register_with_empty_name_throws_DomainException()
    {
        var act = () => Client.Register("", null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_raises_ClientActivated_event()
    {
        var c = Client.Register("Ada", null);
        c.Activate(new DateOnly(2026, 6, 8));
        c.DomainEvents.OfType<ClientActivated>().Should().ContainSingle()
            .Which.ClientId.Should().Be(c.Id);
    }
}
