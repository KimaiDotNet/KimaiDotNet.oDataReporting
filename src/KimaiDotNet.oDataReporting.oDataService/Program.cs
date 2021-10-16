using KimaiDotNet.oDataReporting.oDataService.Models;

using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IEdmModel model0 = EdmModelBuilder.GetEdmModel();
builder.Services.AddControllers().AddOData(opt => opt.Count().Filter().Expand().Select().OrderBy().SetMaxTop(5)
                    .AddRouteComponents(model0));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KimaiDotNet.oDataReporting.oDataService", Version = "v1" });
});


var app = builder.Build();

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
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KimaiDotNet.oDataReporting.oDataService v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
