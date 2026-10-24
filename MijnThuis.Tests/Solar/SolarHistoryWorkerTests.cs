using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MijnThuis.DataAccess;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;
using Shouldly;
using System.Net;

namespace MijnThuis.Tests.Solar;

public class SolarHistoryWorkerTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task FailedSolarCollectorsDoNotPreventBatteryCollection(HttpStatusCode statusCode)
    {
        using var stopping = new CancellationTokenSource();
        var scopeFactory = A.Fake<IServiceScopeFactory>();
        var scope = A.Fake<IServiceScope>();
        var services = A.Fake<IServiceProvider>();
        var modbus = A.Fake<IModbusService>();
        var failure = new HttpRequestException("SolarEdge request failed", null, statusCode);
        var collectorCount = 0;

        A.CallTo(() => scopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(services);
        A.CallTo(() => services.GetService(typeof(ISolarService))).ReturnsLazily(() =>
        {
            collectorCount++;
            if (collectorCount <= 2)
            {
                throw failure;
            }

            return null;
        });
        A.CallTo(() => services.GetService(typeof(MijnThuisDbContext))).Returns(null);
        A.CallTo(() => services.GetService(typeof(IModbusService))).Returns(modbus);
        A.CallTo(() => modbus.GetBatteryLevel()).ReturnsLazily(() =>
        {
            stopping.Cancel();
            return Task.FromCanceled<BatteryLevel>(stopping.Token);
        });
        using var worker = CreateWorker(scopeFactory);

        await RunUntilCanceled(worker, stopping.Token);

        collectorCount.ShouldBe(3);
        A.CallTo(() => scopeFactory.CreateScope()).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => modbus.GetBatteryLevel()).MustHaveHappenedOnceExactly();
        A.CallTo(() => scope.Dispose()).MustHaveHappened(3, Times.Exactly);
    }

    [Fact]
    public async Task ShutdownCancellationDoesNotStartRemainingCollectors()
    {
        using var stopping = new CancellationTokenSource();
        var scopeFactory = A.Fake<IServiceScopeFactory>();
        A.CallTo(() => scopeFactory.CreateScope()).ReturnsLazily(() =>
        {
            stopping.Cancel();
            throw new OperationCanceledException(stopping.Token);
        });
        using var worker = CreateWorker(scopeFactory);

        await RunUntilCanceled(worker, stopping.Token);

        A.CallTo(() => scopeFactory.CreateScope()).MustHaveHappenedOnceExactly();
    }

    private static BackgroundService CreateWorker(IServiceScopeFactory scopeFactory)
    {
        var workerType = typeof(HomeBatteryChargingHelper).Assembly
            .GetType("MijnThuis.Worker.SolarHistoryWorker", throwOnError: true)!;
        var loggerType = typeof(Logger<>).MakeGenericType(workerType);
        var logger = Activator.CreateInstance(loggerType, NullLoggerFactory.Instance)!;

        return (BackgroundService)Activator.CreateInstance(
            workerType, new ConfigurationBuilder().Build(), scopeFactory, logger)!;
    }

    private static async Task RunUntilCanceled(BackgroundService worker, CancellationToken stoppingToken)
    {
        await worker.StartAsync(stoppingToken);
        var execution = worker.ExecuteTask;
        _ = execution.ShouldNotBeNull();
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await execution.WaitAsync(TimeSpan.FromSeconds(10)));
    }
}
