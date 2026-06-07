using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.Products.Application;
using CoreBanking.Products.Application.Products;
using CoreBanking.Products.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoreBanking.Products.UnitTests;

public sealed class CreateSavingsProductHandlerTests
{
    private static CreateSavingsProductCommand ValidCommand() =>
        new("Basic Savings", "BASIC", "USD", 2, 5.0m, 1, 1, 1, 365);

    [Fact]
    public async Task CreateSavingsProduct_handler_persists_and_returns_id()
    {
        var repo = Substitute.For<ISavingsProductRepository>();
        var uow = Substitute.For<IProductUnitOfWork>();
        var handler = new CreateSavingsProductHandler(repo, uow);

        var id = await handler.Handle(ValidCommand(), default);

        repo.Received(1).Add(Arg.Is<SavingsProduct>(p => p.Name == "Basic Savings"));
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetById_throws_NotFoundException_when_not_found()
    {
        var readRepo = Substitute.For<ISavingsProductReadRepository>();
        readRepo.FindDtoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SavingsProductDto?)null);
        var handler = new GetSavingsProductByIdHandler(readRepo);

        var act = async () => await handler.Handle(new GetSavingsProductByIdQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_returns_dto_when_found()
    {
        var id = Guid.NewGuid();
        var dto = new SavingsProductDto(id, "Basic Savings", "BASIC", "USD", 2, 5.0m, "Active");
        var readRepo = Substitute.For<ISavingsProductReadRepository>();
        readRepo.FindDtoAsync(id, Arg.Any<CancellationToken>()).Returns(dto);
        var handler = new GetSavingsProductByIdHandler(readRepo);

        var result = await handler.Handle(new GetSavingsProductByIdQuery(id), default);

        result.Name.Should().Be("Basic Savings");
    }

    [Fact]
    public async Task ListSavingsProducts_returns_all()
    {
        var dtos = new List<SavingsProductDto>
        {
            new(Guid.NewGuid(), "Basic Savings", "BASIC", "USD", 2, 5.0m, "Active"),
            new(Guid.NewGuid(), "Premium Savings", "PREM", "EUR", 2, 7.5m, "Active")
        };
        var readRepo = Substitute.For<ISavingsProductReadRepository>();
        readRepo.ListAsync(Arg.Any<CancellationToken>()).Returns(dtos);
        var handler = new ListSavingsProductsHandler(readRepo);

        var result = await handler.Handle(new ListSavingsProductsQuery(), default);

        result.Should().HaveCount(2);
    }
}
