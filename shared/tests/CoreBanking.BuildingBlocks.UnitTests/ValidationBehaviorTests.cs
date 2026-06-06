using CoreBanking.BuildingBlocks.Application;
using FluentValidation;
using Mediator;
using FluentAssertions;
using Xunit;
using AppValidationException = CoreBanking.BuildingBlocks.Application.ValidationException;

namespace CoreBanking.BuildingBlocks.UnitTests;

public sealed class ValidationBehaviorTests
{
    private sealed record Cmd(string Name) : ICommand<string>;
    private sealed class CmdValidator : AbstractValidator<Cmd>
    {
        public CmdValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class CmdNameAndLengthValidator : AbstractValidator<Cmd>
    {
        public CmdNameAndLengthValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Name).MinimumLength(5).WithMessage("Name must be at least 5 chars.");
        }
    }

    [Fact]
    public async Task Throws_ValidationException_when_invalid()
    {
        var behavior = new ValidationBehavior<Cmd, string>(new[] { new CmdValidator() });
        MessageHandlerDelegate<Cmd, string> next = (_, _) => new ValueTask<string>("ok");
        var act = async () => await behavior.Handle(new Cmd(""), next, default);
        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task Calls_next_when_valid()
    {
        var behavior = new ValidationBehavior<Cmd, string>(new[] { new CmdValidator() });
        MessageHandlerDelegate<Cmd, string> next = (_, _) => new ValueTask<string>("ok");
        var result = await behavior.Handle(new Cmd("Alice"), next, default);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Calls_next_when_no_validators_registered()
    {
        var behavior = new ValidationBehavior<Cmd, string>(Array.Empty<IValidator<Cmd>>());
        MessageHandlerDelegate<Cmd, string> next = (_, _) => new ValueTask<string>("ok");
        var result = await behavior.Handle(new Cmd(""), next, default);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Collects_all_errors_from_all_validators()
    {
        var behavior = new ValidationBehavior<Cmd, string>(new IValidator<Cmd>[]
        {
            new CmdValidator(),               // NotEmpty
            new CmdNameAndLengthValidator()   // NotEmpty + MinimumLength(5)
        });
        MessageHandlerDelegate<Cmd, string> next = (_, _) => new ValueTask<string>("ok");
        var act = async () => await behavior.Handle(new Cmd(""), next, default);
        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        ex.Which.Errors["Name"].Length.Should().BeGreaterThanOrEqualTo(2);
    }
}
