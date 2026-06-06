using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.Products.Application.Products;
using CoreBanking.Products.Infrastructure;
using FluentValidation;
using Mediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateSavingsProductValidator>(ServiceLifetime.Scoped);

builder.Services.AddProductsInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.Run();
