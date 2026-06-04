using KimaiDotNet.Reporting.ODataService;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;
using MarkZither.KimaiDotNet.Reporting.ODataService.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using Polly;
using Polly.Retry;
using Scalar.AspNetCore;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public static class McpServiceExtensions
    {
        public static IServiceCollection AddKimaiMcpServices(this IServiceCollection services)
        {
            services.AddHttpClient(Constants.HttpClients.Kimai)
                .ConfigureHttpClient((serviceProvider, httpClient) =>
                {
                    var kimaiOptions = serviceProvider.GetRequiredService<IOptions<KimaiOptions>>().Value;
                    httpClient.BaseAddress = new Uri(kimaiOptions.Url);
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", kimaiOptions.Password);
                })
                .AddResilienceHandler("KimaiResilience", pipelineBuilder =>
                {
                    pipelineBuilder
                        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                        {
                            MaxRetryAttempts = 3,
                            Delay = TimeSpan.FromSeconds(10),
                            BackoffType = DelayBackoffType.Constant,
                            ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<Exception>(),
                            OnRetry = args =>
                            {
                                var logger = args.Context.GetLogger();
                                if (args.Outcome.Exception != null)
                                {
                                    logger?.LogError(args.Outcome.Exception,
                                        "An exception occurred on retry {RetryAttempt} for {OperationKey}",
                                        args.AttemptNumber + 1,
                                        args.Context.OperationKey);
                                }
                                else
                                {
                                    logger?.LogError("A non success code {StatusCode} was received on retry {RetryAttempt} for {OperationKey}",
                                        (int)args.Outcome.Result!.StatusCode,
                                        args.AttemptNumber + 1,
                                        args.Context.OperationKey);
                                }
                                return ValueTask.CompletedTask;
                            }
                        })
                        .AddChaosStrategies();
                });

            services.AddMcpServer()
                .WithTools<KimaiMcpTools>();

            return services;
        }
    }
}
