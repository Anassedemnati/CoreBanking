using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Infrastructure.Security;
using CoreBanking.Clients.Application.Clients;
using CoreBanking.Clients.Infrastructure;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

builder.Services.AddValidatorsFromAssemblyContaining<RegisterClientValidator>(ServiceLifetime.Scoped);

builder.Services.AddClientsInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"] ?? $"{builder.Configuration["Keycloak:Authority"]}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateAudience = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

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

// Apply this service's EF migrations on startup so it owns and provisions its
// schema (idempotent). Gate off with Database:MigrateOnStartup=false if needed.
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ClientsWriteDbContext>()
        .Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
await app.RunAsync();
