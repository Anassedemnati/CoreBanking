namespace CoreBanking.BuildingBlocks.Domain;
public abstract class Entity
{
    public Guid Id { get; protected init; }
    protected Entity(Guid id) => Id = id;
    public override bool Equals(object? obj) => obj is Entity e && e.GetType() == GetType() && e.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
}
