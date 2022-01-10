using KimaiDotNet.Reporting.ODataService;

using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;

using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;

using MonkeyCache.LiteDB;

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

KimaiOptions kimaiOptions = new KimaiOptions();
builder.Configuration.GetSection(KimaiOptions.Key).Bind(kimaiOptions);
builder.Services.AddHttpClient(Constants.HttpClients.Kimai, httpClient =>
{
    httpClient.BaseAddress = new Uri(kimaiOptions.Url);

    httpClient.DefaultRequestHeaders.Add("X-AUTH-USER", kimaiOptions.Username);
    httpClient.DefaultRequestHeaders.Add("X-AUTH-TOKEN", kimaiOptions.Password);
});
builder.Services.AddMiniProfiler(options =>
    {
        // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

        // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
        options.RouteBasePath = "/profiler";
    });

    var app = builder.Build();

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
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KimaiDotNet.Reporting.ODataService v1"));
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
    Barrel.ApplicationId = "your_unique_name_here2";
    Barrel.EncryptionKey = "SomeKey";
    app.Run();
