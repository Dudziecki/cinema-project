using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Cinema.Tests;

public sealed class ApiIntegrationTests : IClassFixture<CinemaApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CinemaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class CinemaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TEST_ONLY_CINEMA_JWT_KEY_12345678901234567890",
                ["Jwt:Issuer"] = "CinemaApi",
                ["Jwt:Audience"] = "CinemaClient"
            });
        });
    }
}
