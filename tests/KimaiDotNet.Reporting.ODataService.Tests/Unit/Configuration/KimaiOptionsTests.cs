using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.Extensions.Configuration;

namespace KimaiDotNet.Reporting.ODataService.Tests.Unit.Configuration;

public sealed class KimaiOptionsTests
{
    [Test]
    public async Task KimaiOptions_BindsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kimai:Url"] = "http://kimai.example.com/api/",
                ["Kimai:Username"] = "admin",
                ["Kimai:Password"] = "secret"
            })
            .Build();

        var options = new KimaiOptions();
        config.GetSection(KimaiOptions.Key).Bind(options);

        await Assert.That(options.Url).IsEqualTo("http://kimai.example.com/api/");
        await Assert.That(options.Username).IsEqualTo("admin");
        await Assert.That(options.Password).IsEqualTo("secret");
    }

    [Test]
    public async Task KimaiOptions_DefaultsAreEmptyStrings()
    {
        var options = new KimaiOptions();

        await Assert.That(options.Url).IsEqualTo(string.Empty);
        await Assert.That(options.Username).IsEqualTo(string.Empty);
        await Assert.That(options.Password).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task KimaiOptions_Key_IsKimai()
    {
        await Assert.That(KimaiOptions.Key).IsEqualTo("Kimai");
    }
}
