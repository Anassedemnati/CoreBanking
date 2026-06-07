using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Infrastructure;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using FluentValidation;
using Mediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

builder.Services.AddValidatorsFromAssemblyContaining<SubmitSavingsApplicationValidator>(ServiceLifetime.Scoped);

builder.Services.AddSavingsAccountsInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new()
        {
            Title = "CoreBanking – Savings Accounts API",
            Version = "v1",
            Description = "Manages the full savings account lifecycle: submit → approve → activate, " +
                          "plus reject and withdraw paths. Status codes follow Apache Fineract conventions " +
                          "(Submitted=100, Approved=200, Active=300, Withdrawn=400, Rejected=500, Closed=600)."
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
