using Microsoft.AspNetCore.Http.Json;
using PhialeGrid.MockServer.Contracts;
using PhialeGrid.MockServer.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton<DemoGisGridQueryService>();

var app = builder.Build();

app.MapGet("/api/phialegrid/health", () => Results.Ok(new
{
    status = "ok",
    service = "PhialeGrid.MockServer",
}));

app.MapGet("/api/phialegrid/schema", (DemoGisGridQueryService service) =>
{
    return Results.Ok(service.GetSchema());
});

app.MapPost("/api/phialegrid/query", (GridQueryHttpRequest request, DemoGisGridQueryService service) =>
{
    try
    {
        return Results.Ok(service.Execute(request ?? new GridQueryHttpRequest()));
    }
    catch (System.Exception exception)
    {
        return Results.BadRequest(new
        {
            error = exception.Message,
        });
    }
});

app.MapPost("/api/phialegrid/grouped-query", (GridGroupedQueryHttpRequest request, DemoGisGridQueryService service) =>
{
    try
    {
        return Results.Ok(service.ExecuteGrouped(request ?? new GridGroupedQueryHttpRequest()));
    }
    catch (System.Exception exception)
    {
        return Results.BadRequest(new
        {
            error = exception.Message,
        });
    }
});

app.Run();

public partial class Program
{
}
