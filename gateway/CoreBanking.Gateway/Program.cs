using CoreBanking.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS for the browser SPA. Origins are configurable (Cors:AllowedOrigins);
// referenced per-route via the "ui" CorsPolicy in ReverseProxy config.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost"];
builder.Services.AddCors(options =>
    options.AddPolicy("ui", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();
app.MapReverseProxy();
app.Run();

public partial class Program { }
