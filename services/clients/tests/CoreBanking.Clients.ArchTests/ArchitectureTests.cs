using CoreBanking.Clients.Domain;
using CoreBanking.Clients.Application;
using FluentAssertions;
using NetArchTest.Rules;

namespace CoreBanking.Clients.ArchTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_should_not_depend_on_Application()
    {
        var result = Types.InAssembly(typeof(Client).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CoreBanking.Clients.Application",
                "CoreBanking.Clients.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Mediator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(typeof(IClientRepository).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CoreBanking.Clients.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Oracle.EntityFrameworkCore",
                "Confluent.Kafka")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }
}
