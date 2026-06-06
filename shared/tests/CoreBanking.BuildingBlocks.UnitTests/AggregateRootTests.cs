using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

public sealed class AggregateRootTests
{
    private sealed record SampleEvent(Guid Id) : IDomainEvent;
    private sealed class Sample : AggregateRoot
    {
        public Sample(Guid id) : base(id) { }
        public void Do() => Raise(new SampleEvent(Id));
    }

    [Fact]
    public void Raise_buffers_event_until_cleared()
    {
        var a = new Sample(Guid.CreateVersion7());
        a.Do();
        a.DomainEvents.Should().ContainSingle();
        a.ClearDomainEvents();
        a.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var id = Guid.CreateVersion7();
        new Sample(id).Should().Be(new Sample(id));
    }
}
