using CoreBanking.Products.Domain;
using CoreBanking.Products.Application;
using FluentAssertions;
using NetArchTest.Rules;

namespace CoreBanking.Products.ArchTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_should_not_depend_on_Application()
    {
        var result = Types.InAssembly(typeof(SavingsProduct).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CoreBanking.Products.Application",
                "CoreBanking.Products.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Mediator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(typeof(ISavingsProductRepository).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CoreBanking.Products.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Oracle.EntityFrameworkCore",
                "Confluent.Kafka")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }
}
