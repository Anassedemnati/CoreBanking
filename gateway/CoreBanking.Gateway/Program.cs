using CoreBanking.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapReverseProxy();
app.Run();

public partial class Program { }
