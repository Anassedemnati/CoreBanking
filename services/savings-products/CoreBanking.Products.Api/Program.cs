using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Infrastructure.Security;
using CoreBanking.Products.Application.Products;
using CoreBanking.Products.Infrastructure;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateSavingsProductValidator>(ServiceLifetime.Scoped);

builder.Services.AddProductsInfrastructure(builder.Configuration);

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
            Title = "CoreBanking – Savings Products API",
            Version = "v1",
            Description = "Manages savings product templates. A savings product defines the interest " +
                          "settings and currency that savings accounts are based on."
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
await app.RunAsync();
