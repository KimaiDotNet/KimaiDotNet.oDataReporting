using System.Net;
using System.Net.Http.Headers;

namespace KimaiDotNet.Reporting.ODataService.Tests.Integration;

/// <summary>
/// Live integration tests that run against the Docker Compose Kimai environment.
/// Requires: docker compose up -d  (see docker-compose.yml at repo root)
/// Token and URL are seeded deterministically by the kimai-init service.
/// </summary>
[Category("Live")]
public sealed class KimaiContainerTests
{
    private const string KimaiBaseUrl = "http://localhost:8001";
    private const string ApiToken = "kimai-local-integration-test-token";

    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(KimaiBaseUrl),
        DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", ApiToken) }
    };

    [Test]
    public async Task Api_Ping_Returns_Pong()
    {
        var response = await _http.GetAsync("/api/ping");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("pong");
    }

    [Test]
    public async Task Api_Timesheets_Returns_200()
    {
        var response = await _http.GetAsync("/api/timesheets?size=1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Api_Users_Contains_Admin_User()
    {
        var response = await _http.GetAsync("/api/users");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("kimai-admin");
    }

    [Test]
    public async Task Api_Projects_Returns_200()
    {
        var response = await _http.GetAsync("/api/projects");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Api_Customers_Returns_200()
    {
        var response = await _http.GetAsync("/api/customers");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
