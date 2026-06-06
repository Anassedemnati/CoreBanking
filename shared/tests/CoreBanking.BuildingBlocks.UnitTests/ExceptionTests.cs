using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.BuildingBlocks.Application;
using FluentAssertions;
using Xunit;

namespace CoreBanking.BuildingBlocks.UnitTests;

public sealed class ExceptionTests
{
    [Fact]
    public void DomainException_exposes_code_and_message()
    {
        var ex = new DomainException("savings.invalid.state", "bad state");
        ex.Code.Should().Be("savings.invalid.state");
        ex.Message.Should().Be("bad state");
    }

    [Fact]
    public void NotFoundException_formats_message()
    {
        var ex = new NotFoundException("SavingsAccount", Guid.Empty);
        ex.Message.Should().Contain("SavingsAccount");
        ex.Message.Should().Contain(Guid.Empty.ToString());
    }

    [Fact]
    public void ValidationException_exposes_errors_dictionary()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required." } }
        };
        var ex = new ValidationException(errors);
        ex.Errors.Should().ContainKey("Name");
        ex.Errors["Name"].Should().Contain("Name is required.");
    }
}
