using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.Clients.Application.Clients;
using CoreBanking.Clients.Infrastructure;
using FluentValidation;
using Mediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

builder.Services.AddValidatorsFromAssemblyContaining<RegisterClientValidator>(ServiceLifetime.Scoped);

builder.Services.AddClientsInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new()
        {
            Title = "CoreBanking – Clients API",
            Version = "v1",
            Description = "Manages client registration and lifecycle. Clients are people or businesses " +
                          "that have applied (or may apply) for savings accounts."
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
await app.RunAsync();
