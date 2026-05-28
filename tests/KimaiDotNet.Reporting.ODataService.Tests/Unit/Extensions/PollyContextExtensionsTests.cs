using MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;

using Microsoft.Extensions.Logging.Abstractions;

using Polly;

namespace KimaiDotNet.Reporting.ODataService.Tests.Unit.Extensions;

public sealed class PollyContextExtensionsTests
{
    [Test]
    public async Task WithLogger_StoresLogger_OnContext()
    {
        var context = ResilienceContextPool.Shared.Get();
        var logger = NullLogger.Instance;

        context.WithLogger<PollyContextExtensionsTests>(logger);
        var retrieved = context.GetLogger();

        await Assert.That(retrieved).IsEqualTo(logger);

        ResilienceContextPool.Shared.Return(context);
    }

    [Test]
    public async Task GetLogger_ReturnsNull_WhenNotSet()
    {
        var context = ResilienceContextPool.Shared.Get();

        var retrieved = context.GetLogger();

        await Assert.That(retrieved).IsNull();

        ResilienceContextPool.Shared.Return(context);
    }

    [Test]
    public async Task WithLogger_ReturnsContext_ForFluentChaining()
    {
        var context = ResilienceContextPool.Shared.Get();
        var logger = NullLogger.Instance;

        var returned = context.WithLogger<PollyContextExtensionsTests>(logger);

        await Assert.That(returned).IsEqualTo(context);

        ResilienceContextPool.Shared.Return(context);
    }
}
