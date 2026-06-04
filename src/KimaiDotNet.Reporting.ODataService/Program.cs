using KimaiDotNet.Reporting.ODataService;

using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;
using MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;

using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;

using Polly;
using Polly.Retry;
using Scalar.AspNetCore;

// https://gist.github.com/davidfowl/0e0372c3c1d895c3ce195ba983b1e03d
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IEdmModel model0 = EdmModelBuilder.GetEdmModel();
builder.Services.AddControllers().AddOData(opt => opt.Count().Filter().Expand().Select().OrderBy().SetMaxTop(5)
                    .AddRouteComponents(model0));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KimaiDotNet.Reporting.ODataService", Version = "v1" });
});

builder.Services.AddOptions<KimaiOptions>().Bind(
            builder.Configuration.GetSection(KimaiOptions.Key));

builder.Services.AddKimaiMcpServices();

builder.Services.AddMiniProfiler(options =>
    {
        // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

        // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
        options.RouteBasePath = "/profiler";
    });

builder.Services.AddMemoryCache();

    var app = builder.Build();

    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    app.UseMiniProfiler();
    // Use odata route debug, /$odata
    app.UseODataRouteDebug();

    // If you want to use /$openapi, enable the middleware.
    //app.UseODataOpenApi();

    // Add OData /$query middleware
    app.UseODataQueryRequest();

    // Add the OData Batch middleware to support OData $Batch
    app.UseODataBatching();

    // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
                app.UseSwagger();
                app.MapScalarApiReference(options =>
                {
                        options.Title = "KimaiDotNet.Reporting.ODataService API";
                        options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
                });
        }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
    app.Run();

// Required to expose the Program type for WebApplicationFactory<Program> in integration tests
public partial class Program { }