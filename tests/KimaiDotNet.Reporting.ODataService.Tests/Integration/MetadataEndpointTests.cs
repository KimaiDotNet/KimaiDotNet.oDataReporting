using System.Net;

using KimaiDotNet.Reporting.ODataService.Tests;

namespace KimaiDotNet.Reporting.ODataService.Tests.Integration;

[ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerClass)]
public sealed class MetadataEndpointTests(TestWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Test]
    public async Task Get_Metadata_Returns200OK()
    {
        var response = await _client.GetAsync("/$metadata");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Get_Metadata_ReturnsEdmxDocument()
    {
        var response = await _client.GetAsync("/$metadata");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(content).Contains("edmx:Edmx");
    }

    [Test]
    public async Task Get_Metadata_ExposesTimesheetEntitySet()
    {
        var response = await _client.GetAsync("/$metadata");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(content).Contains("Timesheet");
    }
}
