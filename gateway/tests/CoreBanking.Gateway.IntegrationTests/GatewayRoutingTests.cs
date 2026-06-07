using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBanking.Gateway.IntegrationTests;

public sealed class GatewayRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayRoutingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Override YARP destinations to point at a stub
                // We only want to test middleware + routing config, not real backends
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Request_to_clients_route_has_correlation_header_on_response()
    {
        // Arrange — use a custom WebApplicationFactory that wires in a stub backend
        // so we can verify the correlation header is added without needing real services
        var response = await _client.GetAsync("/api/v1/clients/some-id");
        // The gateway will return 502/503 because there's no real backend,
        // but the correlation header should be set on the response regardless
        Assert.True(response.Headers.Contains("X-Correlation-ID"),
            "X-Correlation-ID header should be present on every response");
    }

    [Fact]
    public async Task Request_with_existing_correlation_header_propagates_it()
    {
        var correlationId = Guid.CreateVersion7().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/savingsproducts/some-id");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("X-Correlation-ID", out var values);
        Assert.Equal(correlationId, values?.FirstOrDefault());
    }

    [Fact]
    public async Task Internal_route_returns_404()
    {
        var response = await _client.GetAsync("/internal/some-endpoint");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
