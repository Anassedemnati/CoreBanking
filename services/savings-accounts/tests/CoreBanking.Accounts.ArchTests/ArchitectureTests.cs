using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Application.Abstractions;
using FluentAssertions;
using NetArchTest.Rules;

namespace CoreBanking.Accounts.ArchTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_should_not_depend_on_Application()
    {
        var result = Types.InAssembly(typeof(SavingsAccount).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CoreBanking.Accounts.Application",
                "CoreBanking.Accounts.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Mediator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(typeof(ISavingsAccountRepository).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CoreBanking.Accounts.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Oracle.EntityFrameworkCore",
                "Confluent.Kafka")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }
}
