using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Contrib.Simmy.Latency;
using Polly.Registry;

using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Reflection;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public static class DependencyInjectionExtensions
    {
        private const int ServiceCurrentlyBusySqlErrorNumber = 40501;
        private static readonly Task<bool> NotEnabled = Task.FromResult(false);
        private static readonly Task<double> NoInjectionRate = Task.FromResult<double>(0);
        private static readonly Task<Exception> NoExceptionResult = Task.FromResult<Exception>(null);
        private static readonly Task<HttpResponseMessage> NoHttpResponse = Task.FromResult<HttpResponseMessage>(null);
        private static readonly Task<TimeSpan> NoLatency = Task.FromResult(TimeSpan.Zero);
        public static IPolicyRegistry<string> AddHttpChaosInjectors(this IPolicyRegistry<string> registry)
        {
            foreach (var policyEntry in registry)
            {
                if (policyEntry.Value is IAsyncPolicy<HttpResponseMessage> policy)
                {
                    registry[policyEntry.Key] = policy
                            .WrapAsync(MonkeyPolicy.InjectExceptionAsync(with =>
                                with.Fault(GetException)
                                .InjectionRate(GetInjectionRate)
                                .EnabledWhen(GetEnabled)))
                            .WrapAsync(MonkeyPolicy.InjectLatencyAsync(with =>
                                with.Latency(GetLatency)
                                .InjectionRate(GetInjectionRate)
                                .EnabledWhen(GetEnabled)))
                            .WrapAsync(MonkeyPolicy.InjectLatencyAsync<HttpResponseMessage>(
                                GetLatency,
                                GetInjectionRate,
                                GetEnabled))
                            /*.WrapAsync(MonkeyPolicy.InjectBehaviourAsync<HttpResponseMessage>(
                                (ctx, ct) => RestartNodes(ctx, ct),
                                GetClusterChaosInjectionRate,
                                GetClusterChaosEnabled))
                            .WrapAsync(MonkeyPolicy.InjectBehaviourAsync<HttpResponseMessage>(
                                (ctx, ct) => StopNodes(ctx, ct),
                                GetClusterChaosInjectionRate,
                                GetClusterChaosEnabled))*/;
                }
            }

            return registry;
        }

        private static Task<bool> GetEnabled(Context context, CancellationToken ct)
        {
            var chaosSettings = context.GetOperationChaosSettings();
            if (chaosSettings == null) return NotEnabled;

            return Task.FromResult(chaosSettings.Enabled);
        }

        private static Task<double> GetInjectionRate(Context context, CancellationToken ct)
        {
            var chaosSettings = context.GetOperationChaosSettings();
            if (chaosSettings == null) return NoInjectionRate;

            return Task.FromResult(chaosSettings.InjectionRate);
        }
        private static Task<Exception> GetException(Context context, CancellationToken token)
        {
            var chaosSettings = context.GetOperationChaosSettings();
            if (chaosSettings == null) return NoExceptionResult;

            string exceptionName = chaosSettings.Exception;
            if (string.IsNullOrWhiteSpace(exceptionName)) return NoExceptionResult;

            try
            {
                if (exceptionName == typeof(SqlError).FullName) return Task.FromResult(CreateSqlException() as Exception);

                Type exceptionType = Type.GetType(exceptionName);
                if (exceptionType == null) return NoExceptionResult;

                if (!typeof(Exception).IsAssignableFrom(exceptionType)) return NoExceptionResult;

                var instance = Activator.CreateInstance(exceptionType);
                return Task.FromResult(instance as Exception);
            }
            catch
            {
                return NoExceptionResult;
            }
        }
        private static Task<bool> GetHttpResponseEnabled(Context context)
        {
            if (GetHttpResponseMessage(context, CancellationToken.None) == NoHttpResponse) return NotEnabled;

            return GetEnabled(context, CancellationToken.None);
        }
        private static Task<HttpResponseMessage> GetHttpResponseMessage(Context context, CancellationToken token)
        {
            var chaosSettings = context.GetOperationChaosSettings();
            if (chaosSettings == null) return NoHttpResponse;

            int statusCode = chaosSettings.StatusCode;
            if (statusCode < 200) return NoHttpResponse;

            try
            {
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode));
            }
            catch
            {
                return NoHttpResponse;
            }
        }

        private static Task<TimeSpan> GetLatency(Context context, CancellationToken token)
        {
            var chaosSettings = context.GetOperationChaosSettings();
            if (chaosSettings == null) return NoLatency;

            int milliseconds = chaosSettings.LatencyMs;
            if (milliseconds <= 0) return NoLatency;

            return Task.FromResult(TimeSpan.FromMilliseconds(milliseconds));
        }
        private static SqlException CreateSqlException()
        {
            var collectionConstructor = typeof(SqlErrorCollection)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, //visibility
                    null, //binder
                    new Type[0],
                    null);

            var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
            var errorCollection = (SqlErrorCollection)collectionConstructor.Invoke(null);
            var errorConstructor = typeof(SqlError).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[]
                {
                    typeof (int), typeof (byte), typeof (byte), typeof (string), typeof(string), typeof (string),
                    typeof (int), typeof (uint), typeof(Exception)
                }, null);

            var error = errorConstructor.Invoke(new object[] { ServiceCurrentlyBusySqlErrorNumber, (byte)0, (byte)0, "server", "errMsg", "proccedure", 100, (uint)0, null });
            addMethod.Invoke(errorCollection, new[] { error });

            var constructor = typeof(SqlException)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, //visibility
                    null, //binder
                    new[] { typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid) },
                    null); //param modifiers

            return (SqlException)constructor.Invoke(new object[] { $"Error message: {ServiceCurrentlyBusySqlErrorNumber}", errorCollection, new DataException(), Guid.NewGuid() });
        }
    }
}
