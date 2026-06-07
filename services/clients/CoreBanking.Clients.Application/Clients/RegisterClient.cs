using FluentValidation;
using Mediator;
using CoreBanking.Clients.Domain;

namespace CoreBanking.Clients.Application.Clients;

public sealed record RegisterClientCommand(string DisplayName, string? ExternalId) : ICommand<Guid>;

public sealed class RegisterClientValidator : AbstractValidator<RegisterClientCommand>
{
    public RegisterClientValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
    }
}

public sealed class RegisterClientHandler(IClientRepository repo, IUnitOfWork uow)
    : ICommandHandler<RegisterClientCommand, Guid>
{
    public async ValueTask<Guid> Handle(RegisterClientCommand cmd, CancellationToken ct)
    {
        var client = Client.Register(cmd.DisplayName, cmd.ExternalId);
        repo.Add(client);
        await uow.SaveChangesAsync(ct);
        return client.Id;
    }
}
