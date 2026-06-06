using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.Clients.Api;
using CoreBanking.Clients.Application.Clients;
using CoreBanking.Clients.Infrastructure;
using FluentValidation;
using Mediator;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// Mediator (source-generated, AOT-friendly)
builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

// FluentValidation validators from Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<RegisterClientValidator>(ServiceLifetime.Scoped);

// Clients infrastructure (DbContexts, repos, interceptors, UoW)
builder.Services.AddClientsInfrastructure(builder.Configuration);

// Exception → ProblemDetails
builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();
builder.Services.AddProblemDetails();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapClientsEndpoints();
app.Run();
