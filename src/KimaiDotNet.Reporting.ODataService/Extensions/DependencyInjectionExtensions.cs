using Microsoft.Data.SqlClient;

using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;

using System.Data;
using System.Reflection;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public static class DependencyInjectionExtensions
    {
        private const int ServiceCurrentlyBusySqlErrorNumber = 40501;

        /// <summary>
        /// Adds Polly v8 chaos fault and latency strategies to the pipeline builder.
        /// Settings are resolved at runtime from <see cref="ResilienceContext"/> via
        /// <see cref="SimmyContextExtensions.GetOperationChaosSettings"/>.
        /// </summary>
        public static ResiliencePipelineBuilder<HttpResponseMessage> AddChaosStrategies(
            this ResiliencePipelineBuilder<HttpResponseMessage> pipelineBuilder)
        {
            pipelineBuilder
                .AddChaosFault(new ChaosFaultStrategyOptions
                {
                    FaultGenerator = GetFault,
                    InjectionRateGenerator = GetInjectionRate,
                    EnabledGenerator = GetEnabled
                })
                .AddChaosLatency(new ChaosLatencyStrategyOptions
                {
                    LatencyGenerator = GetLatency,
                    InjectionRateGenerator = GetInjectionRate,
                    EnabledGenerator = GetEnabled
                });

            return pipelineBuilder;
        }

        private static ValueTask<bool> GetEnabled(EnabledGeneratorArguments args)
        {
            var chaosSettings = args.Context.GetOperationChaosSettings();
            if (chaosSettings == null) return ValueTask.FromResult(false);

            return ValueTask.FromResult(chaosSettings.Enabled);
        }

        private static ValueTask<double> GetInjectionRate(InjectionRateGeneratorArguments args)
        {
            var chaosSettings = args.Context.GetOperationChaosSettings();
            if (chaosSettings == null) return ValueTask.FromResult(0.0);

            return ValueTask.FromResult(chaosSettings.InjectionRate);
        }

        private static ValueTask<Exception?> GetFault(FaultGeneratorArguments args)
        {
            var chaosSettings = args.Context.GetOperationChaosSettings();
            if (chaosSettings == null) return ValueTask.FromResult<Exception?>(null);

            string exceptionName = chaosSettings.Exception;
            if (string.IsNullOrWhiteSpace(exceptionName)) return ValueTask.FromResult<Exception?>(null);

            try
            {
                if (exceptionName == typeof(SqlError).FullName)
                    return ValueTask.FromResult<Exception?>(CreateSqlException());

                Type? exceptionType = Type.GetType(exceptionName);
                if (exceptionType == null) return ValueTask.FromResult<Exception?>(null);

                if (!typeof(Exception).IsAssignableFrom(exceptionType))
                    return ValueTask.FromResult<Exception?>(null);

                var instance = Activator.CreateInstance(exceptionType);
                return ValueTask.FromResult(instance as Exception);
            }
            catch
            {
                return ValueTask.FromResult<Exception?>(null);
            }
        }

        private static ValueTask<TimeSpan> GetLatency(LatencyGeneratorArguments args)
        {
            var chaosSettings = args.Context.GetOperationChaosSettings();
            if (chaosSettings == null) return ValueTask.FromResult(TimeSpan.Zero);

            int milliseconds = chaosSettings.LatencyMs;
            if (milliseconds <= 0) return ValueTask.FromResult(TimeSpan.Zero);

            return ValueTask.FromResult(TimeSpan.FromMilliseconds(milliseconds));
        }
        private static SqlException CreateSqlException()
        {
            var collectionConstructor = typeof(SqlErrorCollection)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, //visibility
                    null, //binder
                    new Type[0],
                    null);

            var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
            if (collectionConstructor == null || addMethod == null)
            {
                throw new InvalidOperationException("Failed to access SqlErrorCollection internals.");
            }

            var errorCollection = (SqlErrorCollection)collectionConstructor.Invoke(null);
            var errorConstructor = typeof(SqlError).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[]
                {
                    typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string),
                    typeof(int), typeof(uint), typeof(Exception)
                }, null);

            if (errorConstructor == null)
            {
                throw new InvalidOperationException("Failed to access SqlError constructor.");
            }

            var error = errorConstructor.Invoke(new object[]
            {
                ServiceCurrentlyBusySqlErrorNumber,
                (byte)0,
                (byte)0,
                "server",
                "errMsg",
                "procedure",
                100,
                (uint)0,
                new DataException()
            });

            if (error == null)
            {
                throw new InvalidOperationException("Failed to create SqlError instance.");
            }

            addMethod.Invoke(errorCollection, new[] { error });

            var constructor = typeof(SqlException)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, //visibility
                    null, //binder
                    new[] { typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid) },
                    null); //param modifiers

            if (constructor == null)
            {
                throw new InvalidOperationException("Failed to access SqlException constructor.");
            }

            return (SqlException)constructor.Invoke(new object[]
            {
                $"Error message: {ServiceCurrentlyBusySqlErrorNumber}",
                errorCollection,
                new DataException(),
                Guid.NewGuid()
            });
        }
    }
}
