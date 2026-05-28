using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;
using MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;

using Polly;

namespace KimaiDotNet.Reporting.ODataService.Tests.Unit.Extensions;

public sealed class SimmyContextExtensionsTests
{
    [Test]
    public async Task WithChaosSettings_StoresOptions_OnContext()
    {
        var context = ResilienceContextPool.Shared.Get();
        var options = new GeneralChaosOptions { Sentinel = true };

        context.WithChaosSettings(options);
        var retrieved = context.GetChaosSettings();

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Sentinel).IsTrue();

        ResilienceContextPool.Shared.Return(context);
    }

    [Test]
    public async Task GetChaosSettings_ReturnsNull_WhenNotSet()
    {
        var context = ResilienceContextPool.Shared.Get();

        var retrieved = context.GetChaosSettings();

        await Assert.That(retrieved).IsNull();

        ResilienceContextPool.Shared.Return(context);
    }

    [Test]
    public async Task WithChaosSettings_ReturnsContext_ForFluentChaining()
    {
        var context = ResilienceContextPool.Shared.Get();
        var options = new GeneralChaosOptions();

        var returned = context.WithChaosSettings(options);

        await Assert.That(returned).IsEqualTo(context);

        ResilienceContextPool.Shared.Return(context);
    }

    [Test]
    public async Task GetOperationChaosSettings_ReturnsMatchingOperation_ByKey()
    {
        var context = ResilienceContextPool.Shared.Get("Timesheet");

        var opOptions = new OperationChaosOptions { OperationKey = "Timesheet", Enabled = true, InjectionRate = 0.5 };
        var generalOptions = new GeneralChaosOptions
        {
            OperationChaosSettings = [opOptions]
        };

        context.WithChaosSettings(generalOptions);
        var result = context.GetOperationChaosSettings();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.OperationKey).IsEqualTo("Timesheet");
        await Assert.That(result.InjectionRate).IsEqualTo(0.5);

        ResilienceContextPool.Shared.Return(context);
    }

    [Test]
    public async Task GetOperationChaosSettings_ReturnsNull_WhenNoSettingsSet()
    {
        var context = ResilienceContextPool.Shared.Get("Timesheet");

        var result = context.GetOperationChaosSettings();

        await Assert.That(result).IsNull();

        ResilienceContextPool.Shared.Return(context);
    }
}
