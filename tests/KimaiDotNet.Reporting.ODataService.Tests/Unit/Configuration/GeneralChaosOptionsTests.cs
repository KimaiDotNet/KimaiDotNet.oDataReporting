using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

namespace KimaiDotNet.Reporting.ODataService.Tests.Unit.Configuration;

public sealed class GeneralChaosOptionsTests
{
    [Test]
    public async Task GetSettingsFor_ReturnsMatchingOperation_WhenKeyExists()
    {
        var options = new GeneralChaosOptions
        {
            OperationChaosSettings =
            [
                new OperationChaosOptions { OperationKey = "Timesheet", Enabled = true, InjectionRate = 1.0 },
                new OperationChaosOptions { OperationKey = "User", Enabled = false, InjectionRate = 0.0 }
            ]
        };

        var result = options.GetSettingsFor("Timesheet");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.OperationKey).IsEqualTo("Timesheet");
        await Assert.That(result.Enabled).IsTrue();
        await Assert.That(result.InjectionRate).IsEqualTo(1.0);
    }

    [Test]
    public async Task GetSettingsFor_ReturnsNull_WhenKeyNotFound()
    {
        var options = new GeneralChaosOptions
        {
            OperationChaosSettings =
            [
                new OperationChaosOptions { OperationKey = "Timesheet" }
            ]
        };

        var result = options.GetSettingsFor("NonExistent");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetSettingsFor_ReturnsNull_WhenOperationChaosSettingsIsNull()
    {
        var options = new GeneralChaosOptions
        {
            OperationChaosSettings = null!
        };

        var result = options.GetSettingsFor("Timesheet");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GeneralChaosOptions_AutomaticChaosInjectionEnabled_DefaultsToFalse()
    {
        var options = new GeneralChaosOptions();

        await Assert.That(options.AutomaticChaosInjectionEnabled).IsFalse();
    }
}
